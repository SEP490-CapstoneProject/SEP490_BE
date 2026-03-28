using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Payment.Infrastructure.Providers.PayOS.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace Payment.Infrastructure.Providers.PayOS;

/// <summary>
/// HTTP client for PayOS API with Polly retry, circuit breaker, and timeout policies.
/// Configured in Program.cs with resilience patterns.
/// </summary>
public class PayOSHttpClient
{
    private readonly HttpClient _httpClient;
    private readonly PayOSSettings _settings;
    private readonly ILogger<PayOSHttpClient> _logger;

    public PayOSHttpClient(
        HttpClient httpClient,
        IOptions<PayOSSettings> settings,
        ILogger<PayOSHttpClient> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
        
        // Set headers for all requests
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("x-client-id", _settings.ClientId);
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _settings.ApiKey);
    }

    /// <summary>
    /// Creates payment link via PayOS API.
    /// POST /v2/payment-requests
    /// </summary>
    public async Task<CreatePaymentLinkResponse> CreatePaymentLinkAsync(CreatePaymentLinkRequest request)
    {
        _logger.LogInformation("Creating PayOS payment link for orderCode: {OrderCode}, amount: {Amount}",
            request.OrderCode, request.Amount);

        try
        {
            var response = await _httpClient.PostAsJsonAsync("/v2/payment-requests", request);
            
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<CreatePaymentLinkResponse>();
            
            if (result == null || result.Code != "00")
            {
                _logger.LogError("PayOS API error: {Code} - {Description}", 
                    result?.Code, result?.Description);
                throw new InvalidOperationException($"PayOS API error: {result?.Description}");
            }
            
            _logger.LogInformation("PayOS payment link created successfully. PaymentLinkId: {PaymentLinkId}, CheckoutUrl: {CheckoutUrl}",
                result.Data.PaymentLinkId, result.Data.CheckoutUrl?.Substring(0, 50) + "...");
            
            return result;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error calling PayOS API");
            throw;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON deserialization error from PayOS API");
            throw;
        }
    }

    /// <summary>
    /// Gets payment information from PayOS API for verification.
    /// GET /v2/payment-requests/{orderCode}
    /// CRITICAL: Used to re-verify webhook data for fraud prevention.
    /// </summary>
    public async Task<PaymentLinkData?> GetPaymentInfoAsync(long orderCode)
    {
        _logger.LogInformation("Verifying payment with PayOS API. OrderCode: {OrderCode}", orderCode);

        try
        {
            var response = await _httpClient.GetAsync($"/v2/payment-requests/{orderCode}");
            
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogWarning("Payment not found in PayOS. OrderCode: {OrderCode}", orderCode);
                return null;
            }
            
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<CreatePaymentLinkResponse>();
            
            if (result == null || result.Code != "00")
            {
                _logger.LogWarning("PayOS API verification failed: {Code} - {Description}",
                    result?.Code, result?.Description);
                return null;
            }
            
            _logger.LogInformation("PayOS verification successful. OrderCode: {OrderCode}, Status: {Status}, Amount: {Amount}",
                orderCode, result.Data.Status, result.Data.Amount);
            
            return result.Data;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error verifying payment with PayOS API");
            throw;
        }
    }

    /// <summary>
    /// Cancels payment link via PayOS API.
    /// POST /v2/payment-requests/{orderCode}/cancel
    /// </summary>
    public async Task<bool> CancelPaymentAsync(long orderCode, string? cancellationReason = null)
    {
        _logger.LogInformation("Cancelling PayOS payment. OrderCode: {OrderCode}, Reason: {Reason}",
            orderCode, cancellationReason);

        try
        {
            var requestBody = new { cancellationReason };
            var response = await _httpClient.PostAsJsonAsync($"/v2/payment-requests/{orderCode}/cancel", requestBody);
            
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<CreatePaymentLinkResponse>();
            
            if (result == null || result.Code != "00")
            {
                _logger.LogWarning("PayOS cancellation failed: {Code} - {Description}",
                    result?.Code, result?.Description);
                return false;
            }
            
            _logger.LogInformation("PayOS payment cancelled successfully. OrderCode: {OrderCode}", orderCode);
            return true;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error cancelling payment with PayOS API");
            return false;
        }
    }
}

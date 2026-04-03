using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;
using Payment.Infrastructure.Providers.PayOS.Models;
using System.Text.Json;

namespace Payment.Infrastructure.Providers.PayOS;

/// <summary>
/// PayOS payment provider implementation (fintech-grade).
/// Supports: payment creation, webhook validation, API verification.
/// </summary>
public class PayOSProvider : IPaymentProvider
{
    private readonly PayOSHttpClient _httpClient;
    private readonly PayOSSettings _settings;
    private readonly ILogger<PayOSProvider> _logger;

    public PayOSProvider(
        PayOSHttpClient httpClient,
        IOptions<PayOSSettings> settings,
        ILogger<PayOSProvider> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Creates payment link via PayOS API.
    /// </summary>
    public async Task<CreatePaymentResult> CreatePaymentAsync(PaymentEntity payment)
    {
        _logger.LogInformation("Creating PayOS payment for userId: {UserId}, planId: {PlanId}, amount: {Amount}",
            payment.UserId, payment.PlanId, payment.Amount);

        var request = new CreatePaymentLinkRequest
        {
            OrderCode = long.Parse(payment.OrderCode),
            Amount = (int)payment.Amount,  // VND has no decimals
            Description = $"Payment for subscription {payment.SubscriptionId}",
            ReturnUrl = _settings.ReturnUrl,
            CancelUrl = _settings.CancelUrl
        };

        var response = await _httpClient.CreatePaymentLinkAsync(request);

        _logger.LogInformation("PayOS payment created. OrderCode: {OrderCode}, PaymentLinkId: {PaymentLinkId}",
            payment.OrderCode, response.Data.PaymentLinkId);

        return new CreatePaymentResult(
            CheckoutUrl: response.Data.CheckoutUrl,
            OrderCode: payment.OrderCode,
            PaymentLinkId: response.Data.PaymentLinkId,
            ExpiresAt: DateTime.UtcNow.AddMinutes(15)
        );
    }

    /// <summary>
    /// Validates webhook signature on RAW body (CRITICAL: must validate before parsing).
    /// </summary>
    public bool ValidateSignature(string rawBody, string signature, string? clientId = null)
    {
        _logger.LogDebug("Validating PayOS webhook signature. Signature: {Signature}",
            signature?.Substring(0, 10) + "...");

        var isValid = PayOSSignatureValidator.Validate(rawBody, signature, _settings.ChecksumKey);

        if (!isValid)
        {
            _logger.LogWarning("PayOS webhook signature validation FAILED. Signature: {Signature}",
                signature?.Substring(0, 10) + "...");
        }
        else
        {
            _logger.LogDebug("PayOS webhook signature validation PASSED");
        }

        return isValid;
    }

    /// <summary>
    /// Parses webhook data from raw JSON body (AFTER signature validation).
    /// </summary>
    public WebhookResult ParseWebhookData(string rawBody)
    {
        _logger.LogDebug("Parsing PayOS webhook data");

        try
        {
            var webhook = JsonSerializer.Deserialize<WebhookData>(rawBody);

            if (webhook == null || webhook.Data == null)
            {
                _logger.LogError("Failed to deserialize PayOS webhook data");
                return new WebhookResult { IsValid = false };
            }

            var result = new WebhookResult
            {
                IsValid = true,
                IsSuccess = webhook.Code == "00" && webhook.Data.Code == "00",
                OrderCode = webhook.Data.OrderCode.ToString(),
                TransactionId = webhook.Data.Reference,
                Amount = webhook.Data.Amount,
                Currency = webhook.Data.Currency,
                ErrorMessage = webhook.Code != "00" ? webhook.Description : null,
                RawData = rawBody
            };

            _logger.LogInformation("PayOS webhook parsed. OrderCode: {OrderCode}, Success: {Success}, Amount: {Amount}",
                result.OrderCode, result.IsSuccess, result.Amount);

            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "JSON parsing error for PayOS webhook");
            return new WebhookResult
            {
                IsValid = false,
                ErrorMessage = $"JSON parsing error: {ex.Message}",
                RawData = rawBody
            };
        }
    }

    /// <summary>
    /// Re-verifies payment with PayOS API for fraud prevention (MANDATORY).
    /// Called after webhook to ensure data integrity.
    /// </summary>
    public async Task<PaymentInfo?> VerifyPaymentAsync(string orderCode)
    {
        _logger.LogInformation("Verifying payment with PayOS API. OrderCode: {OrderCode}", orderCode);

        if (!long.TryParse(orderCode, out var orderCodeLong))
        {
            _logger.LogWarning("Invalid orderCode format: {OrderCode}", orderCode);
            return null;
        }

        var paymentData = await _httpClient.GetPaymentInfoAsync(orderCodeLong);

        if (paymentData == null)
        {
            _logger.LogWarning("Payment not found in PayOS API. OrderCode: {OrderCode}", orderCode);
            return null;
        }

        var paymentInfo = new PaymentInfo
        {
            OrderCode = orderCode,
            Amount = paymentData.Amount,
            Currency = paymentData.Currency,
            Status = paymentData.Status,
            TransactionId = null,  // PayOS doesn't provide this in payment info
            Description = paymentData.Description,
            AccountNumber = paymentData.AccountNumber
        };

        _logger.LogInformation("PayOS verification successful. OrderCode: {OrderCode}, Status: {Status}, Amount: {Amount}",
            orderCode, paymentInfo.Status, paymentInfo.Amount);

        return paymentInfo;
    }
}

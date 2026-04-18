using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;
using Payment.Infrastructure.Providers.PayOS.Models;
using System.Security.Cryptography;
using System.Text;
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

        var amountInVnd = NormalizeAmountToVnd(payment.Amount);
        var paymentDescription = BuildDescription(payment.SubscriptionId, payment.PlanId);
        var returnUrl = BuildCallbackUrl(_settings.ReturnUrl, payment);
        var cancelUrl = BuildCallbackUrl(_settings.CancelUrl, payment);

        var request = new CreatePaymentLinkRequest
        {
            OrderCode = long.Parse(payment.OrderCode),
            Amount = amountInVnd,
            Description = paymentDescription,
            ReturnUrl = returnUrl,
            CancelUrl = cancelUrl,
            Signature = BuildCreatePaymentSignature(
                amountInVnd,
                cancelUrl,
                paymentDescription,
                payment.OrderCode,
                returnUrl)
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
        if (string.IsNullOrWhiteSpace(signature))
        {
            _logger.LogWarning("PayOS webhook signature is empty");
            return false;
        }

        _logger.LogDebug("Validating PayOS webhook signature. Signature: {Signature}",
            signature.Length > 10 ? signature[..10] + "..." : signature);

        var isValid = PayOSSignatureValidator.Validate(rawBody, signature, _settings.ChecksumKey);

        if (!isValid)
        {
            _logger.LogWarning("PayOS webhook signature validation FAILED. Signature: {Signature}",
                signature.Length > 10 ? signature[..10] + "..." : signature);
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

    private static int NormalizeAmountToVnd(decimal amount)
    {
        var rounded = decimal.Round(amount, 0, MidpointRounding.AwayFromZero);
        if (rounded > 0 && rounded < 1000)
        {
            rounded = decimal.Round(amount * 1000, 0, MidpointRounding.AwayFromZero);
        }

        return (int)rounded;
    }

    private static string BuildDescription(int subscriptionId, int planId)
    {
        var description = $"Sub{subscriptionId}-Plan{planId}";
        if (description.Length > 9)
        {
            description = $"S{subscriptionId}P{planId}";
        }

        return description.Length > 25 ? description[..25] : description;
    }

    private string BuildCreatePaymentSignature(
        int amount,
        string? cancelUrl,
        string description,
        string orderCode,
        string? returnUrl)
    {
        var signaturePayload =
            $"amount={amount}&cancelUrl={cancelUrl}&description={description}&orderCode={orderCode}&returnUrl={returnUrl}";

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_settings.ChecksumKey));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(signaturePayload));
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    private static string BuildCallbackUrl(string? baseUrl, PaymentEntity payment)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return string.Empty;
        }

        var separator = baseUrl.Contains('?') ? "&" : "?";
        var orderCode = Uri.EscapeDataString(payment.OrderCode);

        return $"{baseUrl}{separator}paymentId={payment.Id}&subscriptionId={payment.SubscriptionId}&orderCode={orderCode}";
    }
}

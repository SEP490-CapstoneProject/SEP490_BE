using Microsoft.Extensions.Logging;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;

namespace Payment.Application.Services;

/// <summary>
/// Payment verification service (fintech-grade fraud prevention).
/// Re-verifies payment with provider API after webhook to ensure data integrity.
/// MANDATORY for production payment systems.
/// </summary>
public class PaymentVerificationService : IPaymentVerificationService
{
    private readonly IPaymentProvider _paymentProvider;
    private readonly ILogger<PaymentVerificationService> _logger;

    public PaymentVerificationService(
        IPaymentProvider paymentProvider,
        ILogger<PaymentVerificationService> logger)
    {
        _paymentProvider = paymentProvider;
        _logger = logger;
    }

    /// <summary>
    /// Verifies webhook data against provider API.
    /// Detects fraud attempts and data inconsistencies.
    /// </summary>
    public async Task<VerificationResult> VerifyWebhookDataAsync(
        PaymentEntity payment,
        WebhookData webhookData)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["PaymentId"] = payment.Id,
            ["OrderCode"] = webhookData.OrderCode
        });

        _logger.LogInformation("Starting payment verification for orderCode: {OrderCode}", 
            webhookData.OrderCode);

        // Step 1: Call PayOS API to get payment info
        PaymentInfo? paymentInfo;
        try
        {
            paymentInfo = await _paymentProvider.VerifyPaymentAsync(webhookData.OrderCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling PayOS API for verification");
            return VerificationResult.Failure($"API error: {ex.Message}");
        }

        // Step 2: Check if payment found
        if (paymentInfo == null)
        {
            _logger.LogWarning("Payment not found in PayOS API. OrderCode: {OrderCode}", 
                webhookData.OrderCode);
            return VerificationResult.Failure("Payment not found in provider");
        }

        // Step 3: Verify amount (CRITICAL - fraud detection)
        // PayOS returns integer VND while DB stores decimal plan price (e.g. 9.99 -> 9990).
        var expectedProviderAmount = NormalizeAmountToProviderUnit(payment.Amount);
        var providerAmount = NormalizeAmountToProviderUnit(paymentInfo.Amount);
        if (Math.Abs(providerAmount - expectedProviderAmount) > 0.01m)
        {
            _logger.LogCritical("FRAUD ALERT: Amount mismatch for OrderCode {OrderCode}. " +
                "Webhook: {WebhookAmount}, API: {ApiAmount}, DB: {DbAmount}, DBNormalized: {DbNormalized}",
                webhookData.OrderCode, webhookData.Amount, paymentInfo.Amount, payment.Amount, expectedProviderAmount);

            return VerificationResult.Failure(
                $"Amount mismatch - Expected: {expectedProviderAmount}, API: {providerAmount}");
        }

        // Step 4: Verify status
        if (paymentInfo.Status != "PAID" && webhookData.Status == "PAID")
        {
            _logger.LogWarning("Status mismatch for OrderCode {OrderCode}. " +
                "Webhook: {WebhookStatus}, API: {ApiStatus}",
                webhookData.OrderCode, webhookData.Status, paymentInfo.Status);

            return VerificationResult.Failure(
                $"Status mismatch - Webhook: {webhookData.Status}, API: {paymentInfo.Status}");
        }

        // Step 5: All checks passed
        _logger.LogInformation("Payment verification successful for OrderCode {OrderCode}", 
            webhookData.OrderCode);

        return VerificationResult.Success();
    }

    public async Task<VerificationResult> VerifyByOrderCodeAsync(string orderCode)
    {
        _logger.LogInformation("Verifying payment by OrderCode: {OrderCode}", orderCode);

        try
        {
            var paymentInfo = await _paymentProvider.VerifyPaymentAsync(orderCode);

            if (paymentInfo == null)
            {
                return VerificationResult.Failure("Payment not found in provider");
            }

            var result = VerificationResult.Success();
            result.ActualAmount = paymentInfo.Amount;
            result.ActualStatus = paymentInfo.Status;

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying payment by OrderCode {OrderCode}", orderCode);
            return VerificationResult.Failure($"API error: {ex.Message}");
        }
    }

    private static decimal NormalizeAmountToProviderUnit(decimal amount)
    {
        var rounded = decimal.Round(amount, 0, MidpointRounding.AwayFromZero);
        if (rounded > 0 && rounded < 1000)
        {
            rounded = decimal.Round(amount * 1000, 0, MidpointRounding.AwayFromZero);
        }

        return rounded;
    }
}

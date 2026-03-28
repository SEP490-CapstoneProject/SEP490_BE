using Payment.Application.DTOs;
using Payment.Domain.Entities;

namespace Payment.Application.Interfaces;

/// <summary>
/// Payment provider abstraction. Supports both legacy (query-based) and modern (JSON-based) providers.
/// Fintech-grade: Includes raw body validation and API verification methods.
/// </summary>
public interface IPaymentProvider
{
    /// <summary>
    /// Creates payment and returns structured result with checkout URL.
    /// Replaces CreatePaymentUrlAsync (string return) for type safety.
    /// </summary>
    Task<CreatePaymentResult> CreatePaymentAsync(PaymentEntity payment);
    
    /// <summary>
    /// Validates webhook signature on RAW body BEFORE parsing.
    /// CRITICAL: Must validate before JSON deserialization to prevent tampering.
    /// </summary>
    /// <param name="rawBody">Raw request body as string (not yet parsed)</param>
    /// <param name="signature">Signature from header (e.g., x-signature)</param>
    /// <param name="clientId">Optional client ID for validation</param>
    bool ValidateSignature(string rawBody, string signature, string? clientId = null);
    
    /// <summary>
    /// Parses webhook data from raw body AFTER signature validation.
    /// Supports both JSON (PayOS) and query params (Legacy VNPay/MoMo).
    /// </summary>
    WebhookResult ParseWebhookData(string rawBody);
    
    /// <summary>
    /// Re-verifies payment with provider API for fraud prevention.
    /// MANDATORY: Called after webhook to ensure data integrity.
    /// </summary>
    /// <param name="orderCode">Order code to verify</param>
    /// <returns>Payment info from provider API or null if not found</returns>
    Task<PaymentInfo?> VerifyPaymentAsync(string orderCode);
}

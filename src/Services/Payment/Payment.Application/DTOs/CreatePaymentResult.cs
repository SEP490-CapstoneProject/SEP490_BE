namespace Payment.Application.DTOs;

/// <summary>
/// Structured result from payment provider after creating payment link.
/// Replaces plain string return value for type safety.
/// </summary>
public record CreatePaymentResult(
    /// <summary>
    /// Payment checkout URL where user completes payment
    /// </summary>
    string CheckoutUrl,
    
    /// <summary>
    /// Order code (ULID-based, globally unique)
    /// </summary>
    string OrderCode,
    
    /// <summary>
    /// Provider-specific payment link ID (e.g., PayOS paymentLinkId)
    /// </summary>
    string? PaymentLinkId,
    
    /// <summary>
    /// When the payment link expires (typically 15 minutes from creation)
    /// </summary>
    DateTime ExpiresAt
);

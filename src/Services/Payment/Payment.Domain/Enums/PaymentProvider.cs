namespace Payment.Domain.Enums;

/// <summary>
/// Payment provider types. PayOS is the active provider, Legacy is for old VNPay/MoMo payments.
/// </summary>
public enum PaymentProvider
{
    /// <summary>
    /// PayOS payment gateway (active)
    /// </summary>
    PayOS = 0,
    
    /// <summary>
    /// Legacy VNPay/MoMo payments (read-only, no new payments)
    /// Migration: All payments before 2026-03-26 marked as Legacy
    /// </summary>
    Legacy = 99
}

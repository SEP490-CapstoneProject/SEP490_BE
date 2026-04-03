namespace Payment.Domain.Enums;

/// <summary>
/// Payment status state machine.
/// Valid transitions:
/// - Pending → Processing → Succeeded/Failed
/// - Pending → Expired (timeout after 15 min)
/// - Pending → Cancelled (user action)
/// </summary>
public enum PaymentStatus
{
    /// <summary>
    /// Payment created, waiting for user to pay
    /// </summary>
    Pending = 0,
    
    /// <summary>
    /// Webhook received, processing in progress (prevents race conditions)
    /// </summary>
    Processing = 1,
    
    /// <summary>
    /// Payment confirmed by provider (Success renamed to Succeeded for consistency)
    /// </summary>
    Succeeded = 2,
    
    /// <summary>
    /// Payment failed (insufficient funds, user cancelled on gateway, etc.)
    /// </summary>
    Failed = 3,
    
    /// <summary>
    /// Payment cancelled by user or admin before completion
    /// </summary>
    Cancelled = 4,
    
    /// <summary>
    /// Payment expired (pending timeout after 15 minutes)
    /// </summary>
    Expired = 5
}

namespace Payment.Domain.Entities;

/// <summary>
/// Tracks processed webhook events for idempotency.
/// UNIQUE constraints prevent duplicate processing.
/// </summary>
public class ProcessedEvent
{
    /// <summary>
    /// Primary key (auto-generated)
    /// </summary>
    public Guid Id { get; set; }
    
    /// <summary>
    /// Correlation ID for tracking (e.g., paymentId)
    /// </summary>
    public string EventId { get; set; } = null!;
    
    /// <summary>
    /// Event type (e.g., "webhook", "manual_reconciliation")
    /// </summary>
    public string EventType { get; set; } = null!;
    
    /// <summary>
    /// SHA256 hash of raw webhook body (UNIQUE - primary idempotency check)
    /// </summary>
    public string EventHash { get; set; } = null!;
    
    /// <summary>
    /// Order code from payment (UNIQUE - secondary idempotency check)
    /// </summary>
    public string OrderCode { get; set; } = null!;
    
    /// <summary>
    /// When event was processed
    /// </summary>
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Optional correlation ID for debugging
    /// </summary>
    public string? CorrelationId { get; set; }
}

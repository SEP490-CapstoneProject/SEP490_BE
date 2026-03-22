namespace Payment.Domain.Entities;

public class ProcessedEvent
{
    public string EventId { get; set; } = null!;
    public string EventType { get; set; } = null!;
    public string? RawHash { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

namespace Subscription.Domain.Entities;

public class ProcessedEvent
{
    public string EventId { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; }
    public string EventType { get; set; } = string.Empty;
}

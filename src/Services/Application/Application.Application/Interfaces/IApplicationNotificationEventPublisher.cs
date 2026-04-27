namespace Application.Application.Interfaces;

public interface IApplicationNotificationEventPublisher
{
    Task PublishAsync(ApplicationNotificationEventPayload payload, CancellationToken cancellationToken = default);
}

public class ApplicationNotificationEventPayload
{
    public string EventId { get; set; } = Guid.NewGuid().ToString("N");
    public string EventType { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public string UserId { get; set; } = string.Empty;
    public string? ActorId { get; set; }
    public string ActorType { get; set; } = "USER";
    public string? ObjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

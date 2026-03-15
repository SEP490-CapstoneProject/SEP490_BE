namespace Notification.Infrastructure.Messaging;

public class NotificationEvent
{
    public string EventType { get; set; } = default!;
    public string UserId { get; set; } = default!;
    public string? ActorId { get; set; }
    public string ActorType { get; set; } = "USER";
    public string? ObjectId { get; set; }
    public string Title { get; set; } = default!;
    public string Content { get; set; } = default!;
    public string Type { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

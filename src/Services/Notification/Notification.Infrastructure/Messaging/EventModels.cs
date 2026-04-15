namespace Notification.Infrastructure.Messaging;
using RecruitmentPlatform.Contracts.Time;

public class NotificationEvent
{
    public string? EventId { get; set; }
    public string EventType { get; set; } = default!;
    public int Version { get; set; } = 1;
    public string UserId { get; set; } = default!;
    public string? ActorId { get; set; }
    public string ActorType { get; set; } = "USER";
    public string? ObjectId { get; set; }
    public string Title { get; set; } = default!;
    public string Content { get; set; } = default!;
    public string Type { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = VietnamTime.Now();
    public NotificationEventUser? Author { get; set; }
    public NotificationEventUser? ReplyToUser { get; set; }
    public int? ReplyToUserId { get; set; }
}

public class NotificationEventUser
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
    public string Role { get; set; } = "USER";
}

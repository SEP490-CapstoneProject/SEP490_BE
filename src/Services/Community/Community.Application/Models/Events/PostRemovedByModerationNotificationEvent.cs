using Community.Application.Helpers;

namespace Community.Application.Models.Events;

public sealed class PostRemovedByModerationNotificationEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString("N");
    public string EventType { get; set; } = "post.report.removed";
    public int Version { get; set; } = 1;
    public string UserId { get; set; } = string.Empty; // Post owner (notification recipient)
    public string? ActorId { get; set; } // Admin/Moderator reviewer
    public string ActorType { get; set; } = "ADMIN";
    public string? ObjectId { get; set; } // PostId
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = "COMMUNITY_MODERATION";
    public DateTime CreatedAt { get; set; } = DateTimeHelper.GetVietnamTime();
}

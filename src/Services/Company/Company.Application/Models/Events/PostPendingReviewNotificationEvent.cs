using Company.Application.Helpers;

namespace Company.Application.Models.Events;

public sealed class PostPendingReviewNotificationEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString("N");
    public string EventType { get; set; } = "post.pending.review";
    public int Version { get; set; } = 1;
    public string UserId { get; set; } = string.Empty;
    public string? ActorId { get; set; }
    public string ActorType { get; set; } = "SYSTEM";
    public string? ObjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = "POST_PENDING_REVIEW";
    public string PostType { get; set; } = "Company";
    public DateTime CreatedAt { get; set; } = DateTimeHelper.GetVietnamTime();
}

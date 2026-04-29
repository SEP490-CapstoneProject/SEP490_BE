using Company.Application.Helpers;

namespace Company.Application.Models.Events;

public sealed class PostApprovedNotificationEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString("N");
    public string EventType { get; set; } = "post.approved";
    public int Version { get; set; } = 1;
    public string UserId { get; set; } = string.Empty;
    public string ActorId { get; set; } = "ADMIN";
    public string ActorType { get; set; } = "ADMIN";
    public string? ObjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = "POST_APPROVED";
    public string PostType { get; set; } = "Company";
    public string? ApproverNotes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTimeHelper.GetVietnamTime();
}

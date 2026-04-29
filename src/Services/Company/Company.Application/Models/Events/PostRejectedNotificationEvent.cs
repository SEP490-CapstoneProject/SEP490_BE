using Company.Application.Helpers;

namespace Company.Application.Models.Events;

public sealed class PostRejectedNotificationEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString("N");
    public string EventType { get; set; } = "post.rejected";
    public int Version { get; set; } = 1;
    public string UserId { get; set; } = string.Empty;
    public string ActorId { get; set; } = "ADMIN";
    public string ActorType { get; set; } = "ADMIN";
    public string? ObjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = "POST_REJECTED";
    public string PostType { get; set; } = "Company";
    public DateTime CreatedAt { get; set; } = DateTimeHelper.GetVietnamTime();
}

using Community.Application.Helpers;

namespace Community.Application.Models.Events;

public sealed class PostFavoriteNotificationEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString("N");
    public string EventType { get; set; } = "post.favorite";
    public int Version { get; set; } = 1;
    public string UserId { get; set; } = string.Empty; // Post owner (notification recipient)
    public string? ActorId { get; set; } // Person who favorited
    public string ActorType { get; set; } = "USER";
    public string? ObjectId { get; set; } // PostId
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = "POST_FAVORITE";
    public DateTime CreatedAt { get; set; } = DateTimeHelper.GetVietnamTime();
}

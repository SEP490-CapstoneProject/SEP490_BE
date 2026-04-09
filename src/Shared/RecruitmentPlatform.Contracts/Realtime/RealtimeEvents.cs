namespace RecruitmentPlatform.Contracts.Realtime;

public abstract class RealtimeEventBase
{
    public string EventId { get; set; } = Guid.NewGuid().ToString("N");
    public string EventType { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public sealed class RealtimeUserDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
    public string Role { get; set; } = "USER";
}

public sealed class CommentCreatedEvent : RealtimeEventBase
{
    public int PostId { get; set; }
    public int CommentId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? ActorId { get; set; }
    public string ActorType { get; set; } = "USER";
    public string? ObjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public RealtimeUserDto? Author { get; set; }
}

public sealed class ReplyCreatedEvent : RealtimeEventBase
{
    public int PostId { get; set; }
    public int CommentId { get; set; }
    public int? ParentCommentId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string? ActorId { get; set; }
    public string ActorType { get; set; } = "USER";
    public string? ObjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int? ReplyToUserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public RealtimeUserDto? Author { get; set; }
    public RealtimeUserDto? ReplyToUser { get; set; }
}

public sealed class NotificationActorDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}

public sealed class PostFavoriteChangedEvent : RealtimeEventBase
{
    public int PostId { get; set; }
    public int UserId { get; set; }
    public string Action { get; set; } = "FAVORITE"; // "FAVORITE" or "UNFAVORITE"
    public int NewFavoriteCount { get; set; }
}

public sealed class NotificationCreatedEvent : RealtimeEventBase
{
    public int NotificationId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? ObjectId { get; set; }
    public NotificationActorDto? Actor { get; set; }
    public bool IsRead { get; set; }
}

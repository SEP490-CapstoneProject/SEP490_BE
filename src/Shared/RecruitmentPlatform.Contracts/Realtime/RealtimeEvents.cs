using System.Text.Json.Serialization;
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
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Avatar { get; set; } = string.Empty;
    [JsonPropertyName("Role")]
    public string Role { get; set; } = "USER";
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
    public string Category { get; set; } = "system";
    public string? ObjectId { get; set; }
    public NotificationActorDto? Actor { get; set; }
    public bool IsRead { get; set; }
}

/// <summary>
/// Fired when user A sends a connection request to user B.
/// Realtime pushes "ConnectionRequested" to group user_{ToUserId}.
/// </summary>
public sealed class ConnectionRequestedEvent : RealtimeEventBase
{
    public int ConnectionId { get; set; }
    public int FromUserId { get; set; }
    public int ToUserId { get; set; }
    public int ProfileId { get; set; }
    public DateTime RequestedAt { get; set; }
}

/// <summary>
/// Fired when user B accepts (MATCHED) a connection request.
/// Realtime pushes "ConnectionAccepted" to group user_{FromUserId}.
/// </summary>
public sealed class ConnectionAcceptedEvent : RealtimeEventBase
{
    public int ConnectionId { get; set; }
    public int FromUserId { get; set; }
    public int ToUserId { get; set; }
    public DateTime AcceptedAt { get; set; }
}

/// <summary>
/// Fired when a new chat message is created.
/// Realtime pushes "NewMessageNotification" to group user_{ToUserId}.
/// </summary>
public sealed class NewMessageNotificationEvent : RealtimeEventBase
{
    public int MessageId { get; set; }
    public int RoomId { get; set; }
    public int FromUserId { get; set; }
    public int ToUserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime SentAt { get; set; }
}

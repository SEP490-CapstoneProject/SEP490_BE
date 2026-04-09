namespace Notification.Application.DTOs;

public class UserNotificationDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Content { get; set; } = default!;
    public string Type { get; set; } = default!;
    public string? ObjectId { get; set; }
    public ActorDto? Actor { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
}

public class ActorDto
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? AvatarUrl { get; set; }
}

public class CursorPagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int? NextCursor { get; set; }
    public bool HasMore { get; set; }
}

public class NotificationEventDto
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

public class NotificationCreatedEventDto
{
    public string EventId { get; set; } = Guid.NewGuid().ToString("N");
    public string EventType { get; set; } = "notification.created";
    public int Version { get; set; } = 1;
    public int NotificationId { get; set; }
    public string UserId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Content { get; set; } = default!;
    public string Type { get; set; } = default!;
    public string? ObjectId { get; set; }
    public ActorDto? Actor { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
}

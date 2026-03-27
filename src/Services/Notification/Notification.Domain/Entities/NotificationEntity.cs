namespace Notification.Domain.Entities;

public class NotificationEntity
{
    public int Id { get; set; }
    public string UserId { get; set; } = default!;
    public string Title { get; set; } = default!;
    public string Content { get; set; } = default!;
    public string Type { get; set; } = default!;
    public string? ObjectId { get; set; }
    public string? ActorId { get; set; }
    public string ActorType { get; set; } = "SYSTEM";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; } = false;
}

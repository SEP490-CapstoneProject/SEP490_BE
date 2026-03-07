using RecruitmentPlatform.Common;

namespace Connection.Domain.Entities;

public class Message : BaseEntity
{
    public int UserId { get; set; }
    public int MessageRoomId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int Status { get; set; }

    // Navigation properties
    public virtual Room? Room { get; set; }
}
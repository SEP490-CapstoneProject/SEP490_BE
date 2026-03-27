using RecruitmentPlatform.Common;

namespace Connection.Domain.Entities;

public class Room : BaseEntity
{
    public int ConnectionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastMessAt { get; set; }

    // Navigation properties
    public virtual Connection? Connection { get; set; }
    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();
}
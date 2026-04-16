using RecruitmentPlatform.Common;

namespace Connection.Domain.Entities;

public class Connection : BaseEntity
{
    public int UserIdFrom { get; set; }
    public int UserIdTo { get; set; }
    public int ProfileId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int BlockId { get; set; } = 0;
    public DateTime CreateAt { get; set; }
    public DateTime? ConnectionAt { get; set; }

    // Navigation properties
    public virtual ICollection<Room> Rooms { get; set; } = new List<Room>();
}
namespace Connection.Application.DTOs;

public class RoomSummaryRaw
{
    public int RoomId { get; set; }
    public int ProfileId { get; set; }
    public int ConnectionId { get; set; }
    public int UserIdFrom { get; set; }
    public int UserIdTo { get; set; }
    public string? LastContent { get; set; }
    public DateTime? LastAt { get; set; }
    public int UnreadCount { get; set; }
}

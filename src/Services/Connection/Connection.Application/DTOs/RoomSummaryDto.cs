namespace Connection.Application.DTOs;

public class RoomSummaryDto
{
    public int RoomId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Avatar { get; set; }
    public string? CoverImage { get; set; }
    public string Role { get; set; } = "USER";
    public string? LastContent { get; set; }
    public DateTime? LastAt { get; set; }
    public int UnreadCount { get; set; }
}

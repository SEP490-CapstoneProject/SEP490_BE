namespace Connection.Application.DTOs;

public class MessageDto
{
    public int Id { get; set; }
    public int MessageRoomId { get; set; }
    public int UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = "UNREAD";
}

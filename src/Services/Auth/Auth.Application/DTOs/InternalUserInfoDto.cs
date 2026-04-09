namespace Auth.Application.DTOs;

public class InternalUserInfoDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreateAt { get; set; }
}

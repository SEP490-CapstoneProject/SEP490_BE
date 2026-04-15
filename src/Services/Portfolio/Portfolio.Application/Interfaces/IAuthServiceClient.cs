namespace Portfolio.Application.Interfaces;

public class AuthInternalUserDto
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreateAt { get; set; }
}

public interface IAuthServiceClient
{
    Task<Dictionary<int, AuthInternalUserDto>> GetUsersByIdsAsync(IEnumerable<int> userIds);
}

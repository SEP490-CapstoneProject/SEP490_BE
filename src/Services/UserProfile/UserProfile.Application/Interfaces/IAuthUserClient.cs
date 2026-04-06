using UserProfile.Application.DTOs;

namespace UserProfile.Application.Interfaces;

public interface IAuthUserClient
{
    Task<AuthUserInfoDto?> GetUserByIdAsync(int userId);
    Task<Dictionary<int, AuthUserInfoDto>> GetUsersByIdsAsync(IEnumerable<int> userIds);
}

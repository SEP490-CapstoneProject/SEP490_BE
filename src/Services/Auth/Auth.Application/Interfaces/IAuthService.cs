using RecruitmentPlatform.Contracts.Auth;
using Auth.Application.DTOs;

namespace Auth.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> RegisterAsync(RegisterRequest request);
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<LoginResponse> RefreshTokenAsync(string refreshToken);
    Task RevokeTokenAsync(string refreshToken);
    Task ChangePasswordAsync(int userId, ChangePasswordRequest request);
    Task LockUserAsync(int userId);
    Task UnlockUserAsync(int userId);
    Task<IEnumerable<UserDto>> GetAllUsersAsync();
    Task<InternalUserInfoDto?> GetInternalUserInfoByIdAsync(int userId);
    Task<IEnumerable<InternalUserInfoDto>> GetInternalUserInfosByIdsAsync(IEnumerable<int> userIds);
    Task<IEnumerable<InternalUserInfoDto>> GetInternalUserInfosByRolesAsync(IEnumerable<string> roles);
}

using Auth.Domain.Entities;
using RecruitmentPlatform.Contracts.Enums;

namespace Auth.Application.Interfaces;

public interface IAuthRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(int id);
    Task<IEnumerable<User>> GetByIdsAsync(IEnumerable<int> ids);
    Task<IEnumerable<User>> GetByRolesAsync(IEnumerable<UserRole> roles);
    Task<User> CreateAsync(User user);
    Task UpdateAsync(User user);
    Task<RefreshToken?> GetRefreshTokenAsync(string token);
    Task AddRefreshTokenAsync(RefreshToken refreshToken);
    Task RevokeRefreshTokenAsync(string token);
    Task<IEnumerable<User>> GetAllUsersAsync();

    // Password Reset
    Task AddPasswordResetTokenAsync(PasswordResetToken token);
    Task<PasswordResetToken?> GetValidPasswordResetTokenAsync(string email, string token);
    Task InvalidatePasswordResetTokensAsync(string email);
}

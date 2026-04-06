using Auth.Domain.Entities;

namespace Auth.Application.Interfaces;

public interface IAuthRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByIdAsync(int id);
    Task<IEnumerable<User>> GetByIdsAsync(IEnumerable<int> ids);
    Task<User> CreateAsync(User user);
    Task UpdateAsync(User user);
    Task<RefreshToken?> GetRefreshTokenAsync(string token);
    Task AddRefreshTokenAsync(RefreshToken refreshToken);
    Task RevokeRefreshTokenAsync(string token);
    Task<IEnumerable<User>> GetAllUsersAsync();
}

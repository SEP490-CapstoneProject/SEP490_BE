using Auth.Domain.Entities;
using Auth.Application.Interfaces;
using Auth.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using RecruitmentPlatform.Contracts.Enums;

namespace Auth.Infrastructure.Repositories;

public class AuthRepository : IAuthRepository
{
    private readonly AuthDbContext _context;

    public AuthRepository(AuthDbContext context)
    {
        _context = context;
    }

    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<User?> GetByIdAsync(int id)
    {
        return await _context.Users.FindAsync(id);
    }

    public async Task<IEnumerable<User>> GetByIdsAsync(IEnumerable<int> ids)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0)
        {
            return Enumerable.Empty<User>();
        }

        return await _context.Users
            .Where(u => idList.Contains(u.Id))
            .ToListAsync();
    }

    public async Task<IEnumerable<User>> GetByRolesAsync(IEnumerable<UserRole> roles)
    {
        var roleList = roles.Distinct().ToList();
        if (roleList.Count == 0)
        {
            return Enumerable.Empty<User>();
        }

        return await _context.Users
            .Where(u => roleList.Contains(u.Role))
            .ToListAsync();
    }

    public async Task<User> CreateAsync(User user)
    {
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    public async Task UpdateAsync(User user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        _context.Users.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
    {
        return await _context.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.Token == token);
    }

    public async Task AddRefreshTokenAsync(RefreshToken refreshToken)
    {
        _context.RefreshTokens.Add(refreshToken);
        await _context.SaveChangesAsync();
    }

    public async Task RevokeRefreshTokenAsync(string token)
    {
        var refreshToken = await _context.RefreshTokens.FirstOrDefaultAsync(rt => rt.Token == token);
        if (refreshToken != null)
        {
            refreshToken.Revoked = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<User>> GetAllUsersAsync()
    {
        return await _context.Users.ToListAsync();
    }

    // =========== Password Reset ===========

    public async Task AddPasswordResetTokenAsync(PasswordResetToken token)
    {
        _context.PasswordResetTokens.Add(token);
        await _context.SaveChangesAsync();
    }

    public async Task<PasswordResetToken?> GetValidPasswordResetTokenAsync(string email, string token)
    {
        return await _context.PasswordResetTokens
            .Include(t => t.User)
            .Where(t => t.User.Email == email
                     && t.Token == token
                     && !t.IsUsed
                     && t.ExpiredAt > DateTime.UtcNow)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync();
    }

    public async Task InvalidatePasswordResetTokensAsync(string email)
    {
        var tokens = await _context.PasswordResetTokens
            .Include(t => t.User)
            .Where(t => t.User.Email == email && !t.IsUsed)
            .ToListAsync();

        foreach (var t in tokens)
            t.IsUsed = true;

        await _context.SaveChangesAsync();
    }
}

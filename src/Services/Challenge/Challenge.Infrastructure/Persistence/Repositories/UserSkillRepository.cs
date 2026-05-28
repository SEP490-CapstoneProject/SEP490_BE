using Microsoft.EntityFrameworkCore;
using Challenge.Domain.Entities;
using Challenge.Domain.Repositories;
namespace Challenge.Infrastructure.Persistence.Repositories;


public class UserSkillRepository : IUserSkillRepository
{
    private readonly ChallengeDbContext _context;

    public UserSkillRepository(ChallengeDbContext context)
    {
        _context = context;
    }

    public async Task<UserSkill> GetByUserAndSkillAsync(
        int userId, Guid skillId, CancellationToken cancellationToken = default)
    {
        return await _context.UserSkills
            .FirstOrDefaultAsync(u => u.UserId == userId && u.SkillId == skillId, cancellationToken);
    }

    public async Task<IEnumerable<UserSkill>> GetByUserAsync(
        int userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserSkills
            .Where(u => u.UserId == userId)
            .OrderByDescending(u => u.TotalPoints)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<UserSkill>> GetVerifiedByUserAsync(
        int userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserSkills
            .Where(u => u.UserId == userId && u.IsVerified)
            .OrderByDescending(u => u.TotalPoints)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<UserSkill>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.UserSkills
            .OrderByDescending(u => u.TotalPoints)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> GetTotalPointsByUserAsync(
        int userId, CancellationToken cancellationToken = default)
    {
        return await _context.UserSkills
            .Where(u => u.UserId == userId)
            .SumAsync(u => u.TotalPoints, cancellationToken);
    }

    public async Task AddAsync(UserSkill userSkill, CancellationToken cancellationToken = default)
    {
        _context.UserSkills.Add(userSkill);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(UserSkill userSkill, CancellationToken cancellationToken = default)
    {
        _context.UserSkills.Update(userSkill);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(int userId, Guid skillId, CancellationToken cancellationToken = default)
    {
        return await _context.UserSkills
            .AnyAsync(u => u.UserId == userId && u.SkillId == skillId, cancellationToken);
    }
}

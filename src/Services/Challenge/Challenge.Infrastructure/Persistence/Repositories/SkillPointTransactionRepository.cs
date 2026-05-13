using Microsoft.EntityFrameworkCore;
using Challenge.Domain.Entities;
using Challenge.Domain.Repositories;
namespace Challenge.Infrastructure.Persistence.Repositories;


public class SkillPointTransactionRepository : ISkillPointTransactionRepository
{
    private readonly ChallengeDbContext _context;

    public SkillPointTransactionRepository(ChallengeDbContext context)
    {
        _context = context;
    }

    public async Task<SkillPointTransaction> GetByIdAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.SkillPointTransactions.FindAsync(
            new object[] { id }, cancellationToken: cancellationToken);
    }

    public async Task<IEnumerable<SkillPointTransaction>> GetByUserAsync(
        Guid userId, CancellationToken cancellationToken = default)
    {
        return await _context.SkillPointTransactions
            .Where(t => t.UserId == userId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<SkillPointTransaction>> GetByUserAndSkillAsync(
        Guid userId, Guid skillId, CancellationToken cancellationToken = default)
    {
        return await _context.SkillPointTransactions
            .Where(t => t.UserId == userId && t.SkillId == skillId)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<decimal> GetTotalPointsByUserAndSkillAsync(
        Guid userId, Guid skillId, CancellationToken cancellationToken = default)
    {
        return await _context.SkillPointTransactions
            .Where(t => t.UserId == userId && t.SkillId == skillId)
            .SumAsync(t => t.Points, cancellationToken);
    }

    public async Task AddAsync(SkillPointTransaction transaction, CancellationToken cancellationToken = default)
    {
        _context.SkillPointTransactions.Add(transaction);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

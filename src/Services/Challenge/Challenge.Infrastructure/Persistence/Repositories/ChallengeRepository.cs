using Challenge.Domain.Entities;
using Challenge.Domain.Enums;
using Challenge.Domain.Repositories;
using Microsoft.EntityFrameworkCore;
using ChallengeEntity = Challenge.Domain.Entities.Challenge;

namespace Challenge.Infrastructure.Persistence.Repositories;

public class ChallengeRepository : IChallengeRepository
{
    private readonly ChallengeDbContext _context;

    public ChallengeRepository(ChallengeDbContext context)
    {
        _context = context;
    }

    public async Task<ChallengeEntity> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Challenges
            .Include(c => c.CurrentVersion)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<IEnumerable<ChallengeEntity>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Challenges
            .Include(c => c.CurrentVersion)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ChallengeEntity>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var challengeIds = ids.Distinct().ToList();

        if (challengeIds.Count == 0)
        {
            return Array.Empty<ChallengeEntity>();
        }

        return await _context.Challenges
            .Include(c => c.CurrentVersion)
            .Where(c => challengeIds.Contains(c.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ChallengeEntity>> GetByStatusAsync(
        ChallengeStatus status, CancellationToken cancellationToken = default)
    {
        return await _context.Challenges
            .Include(c => c.CurrentVersion)
            .Where(c => c.Status == status)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ChallengeEntity>> GetPublishedAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Challenges
            .Include(c => c.CurrentVersion)
            .Where(c => c.Status == ChallengeStatus.Published)
            .OrderByDescending(c => c.PublishedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<ChallengeEntity>> GetExpiredAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Challenges
            .Include(c => c.CurrentVersion)
            .Where(c => c.Status == ChallengeStatus.Expired)
            .OrderByDescending(c => c.PublishedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ChallengeEntity challenge, CancellationToken cancellationToken = default)
    {
        _context.Challenges.Add(challenge);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ChallengeEntity challenge, CancellationToken cancellationToken = default)
    {
        _context.Challenges.Update(challenge);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var challenge = await GetByIdAsync(id, cancellationToken);
        if (challenge != null)
        {
            _context.Challenges.Remove(challenge);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Challenges.AnyAsync(c => c.Id == id, cancellationToken);
    }
}

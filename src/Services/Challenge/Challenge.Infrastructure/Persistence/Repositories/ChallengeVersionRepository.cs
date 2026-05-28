using Microsoft.EntityFrameworkCore;
using Challenge.Domain.Entities;
using Challenge.Domain.Repositories;

namespace Challenge.Infrastructure.Persistence.Repositories;

public class ChallengeVersionRepository : IChallengeVersionRepository
{
    private readonly ChallengeDbContext _context;

    public ChallengeVersionRepository(ChallengeDbContext context)
    {
        _context = context;
    }

    public async Task<ChallengeVersion> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.ChallengeVersions.Include(cv => cv.Challenge).FirstOrDefaultAsync(cv => cv.Id == id, cancellationToken);

    public Task<ChallengeVersion> GetActiveVersionAsync(Guid challengeId, CancellationToken cancellationToken = default)
        => _context.ChallengeVersions.FirstOrDefaultAsync(cv => cv.ChallengeId == challengeId, cancellationToken)!;

    public Task<IEnumerable<ChallengeVersion>> GetVersionsByChallengeAsync(Guid challengeId, CancellationToken cancellationToken = default)
        => GetByChallengeAsync(challengeId, cancellationToken);

    public async Task<IEnumerable<ChallengeVersion>> GetLatestByChallengeAsync(Guid challengeId, CancellationToken cancellationToken = default)
        => await _context.ChallengeVersions.Where(cv => cv.ChallengeId == challengeId).OrderByDescending(cv => cv.VersionNumber).ToListAsync(cancellationToken);

    public async Task<IEnumerable<ChallengeVersion>> GetByChallengeAsync(Guid challengeId, CancellationToken cancellationToken = default)
        => await _context.ChallengeVersions.Where(cv => cv.ChallengeId == challengeId).OrderByDescending(cv => cv.VersionNumber).ToListAsync(cancellationToken);

    public async Task AddAsync(ChallengeVersion version, CancellationToken cancellationToken = default)
    {
        _context.ChallengeVersions.Add(version);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ChallengeVersion version, CancellationToken cancellationToken = default)
    {
        _context.ChallengeVersions.Update(version);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.ChallengeVersions.AnyAsync(cv => cv.Id == id, cancellationToken);
}

using Microsoft.EntityFrameworkCore;
using Challenge.Domain.Entities;
using Challenge.Domain.Repositories;
using Challenge.Domain.Enums;

namespace Challenge.Infrastructure.Persistence.Repositories;

public class SubmissionRepository : ISubmissionRepository
{
    private readonly ChallengeDbContext _context;

    public SubmissionRepository(ChallengeDbContext context)
    {
        _context = context;
    }

    public async Task<ChallengeSubmission> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.ChallengeSubmissions.Include(cs => cs.Version).FirstOrDefaultAsync(cs => cs.Id == id, cancellationToken);

    public async Task<IEnumerable<ChallengeSubmission>> GetByChallengeAsync(Guid challengeId, CancellationToken cancellationToken = default)
        => await _context.ChallengeSubmissions.Include(cs => cs.Version).Where(cs => cs.ChallengeId == challengeId).OrderByDescending(cs => cs.CreatedAt).ToListAsync(cancellationToken);

    public async Task<IEnumerable<ChallengeSubmission>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => await _context.ChallengeSubmissions.Include(cs => cs.Version).Where(cs => cs.UserId == userId).OrderByDescending(cs => cs.CreatedAt).ToListAsync(cancellationToken);

    public Task<IEnumerable<ChallengeSubmission>> GetByUserAndChallengeAsync(Guid userId, Guid challengeId, CancellationToken cancellationToken = default)
        => Task.FromResult<IEnumerable<ChallengeSubmission>>(new List<ChallengeSubmission>());

    public async Task<int> GetAttemptCountAsync(Guid userId, Guid challengeId, CancellationToken cancellationToken = default)
        => await _context.ChallengeSubmissions.CountAsync(cs => cs.UserId == userId && cs.ChallengeId == challengeId, cancellationToken);

    public async Task AddAsync(ChallengeSubmission submission, CancellationToken cancellationToken = default)
    {
        _context.ChallengeSubmissions.Add(submission);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(ChallengeSubmission submission, CancellationToken cancellationToken = default)
    {
        _context.ChallengeSubmissions.Update(submission);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

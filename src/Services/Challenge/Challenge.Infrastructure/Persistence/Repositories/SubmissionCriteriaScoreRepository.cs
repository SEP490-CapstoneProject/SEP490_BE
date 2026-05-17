using Microsoft.EntityFrameworkCore;
using Challenge.Domain.Entities;
using Challenge.Domain.Repositories;

namespace Challenge.Infrastructure.Persistence.Repositories;

public class SubmissionCriteriaScoreRepository : ISubmissionCriteriaScoreRepository
{
    private readonly ChallengeDbContext _context;

    public SubmissionCriteriaScoreRepository(ChallengeDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(SubmissionCriteriaScore score, CancellationToken cancellationToken = default)
    {
        _context.SubmissionCriteriaScores.Add(score);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

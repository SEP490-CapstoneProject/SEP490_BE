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

    public async Task AddRangeAsync(IEnumerable<SubmissionCriteriaScore> scores, CancellationToken cancellationToken = default)
    {
        var scoreList = scores?.ToList() ?? new List<SubmissionCriteriaScore>();
        if (!scoreList.Any())
        {
            return;
        }

        _context.SubmissionCriteriaScores.AddRange(scoreList);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

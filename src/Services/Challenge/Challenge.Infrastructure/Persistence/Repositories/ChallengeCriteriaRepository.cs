using Challenge.Domain.Entities;
using Challenge.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Challenge.Infrastructure.Persistence.Repositories;

public class ChallengeCriteriaRepository : IChallengeCriteriaRepository
{
    private readonly ChallengeDbContext _context;

    public ChallengeCriteriaRepository(ChallengeDbContext context)
    {
        _context = context;
    }

    public async Task<List<ChallengeCriteria>> GetByVersionAsync(Guid challengeVersionId, CancellationToken cancellationToken = default)
        => await _context.ChallengeCriteria
            .Include(x => x.Criteria)
            .Where(x => x.ChallengeVersionId == challengeVersionId)
            .ToListAsync(cancellationToken);

    public async Task AddRangeAsync(IEnumerable<ChallengeCriteria> challengeCriteria, CancellationToken cancellationToken = default)
    {
        var list = challengeCriteria?.ToList() ?? new List<ChallengeCriteria>();
        if (!list.Any())
        {
            return;
        }

        _context.ChallengeCriteria.AddRange(list);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

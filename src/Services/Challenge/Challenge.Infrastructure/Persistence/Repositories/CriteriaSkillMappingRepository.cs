using Challenge.Domain.Entities;
using Challenge.Domain.Repositories;

namespace Challenge.Infrastructure.Persistence.Repositories;

public class CriteriaSkillMappingRepository : ICriteriaSkillMappingRepository
{
    private readonly ChallengeDbContext _context;

    public CriteriaSkillMappingRepository(ChallengeDbContext context)
    {
        _context = context;
    }

    public async Task AddRangeAsync(IEnumerable<CriteriaSkillMapping> mappings, CancellationToken cancellationToken = default)
    {
        var list = mappings?.ToList() ?? new List<CriteriaSkillMapping>();
        if (!list.Any())
        {
            return;
        }

        _context.CriteriaSkillMappings.AddRange(list);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

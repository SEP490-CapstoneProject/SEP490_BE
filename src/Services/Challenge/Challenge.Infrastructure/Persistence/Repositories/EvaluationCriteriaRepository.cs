using Challenge.Domain.Entities;
using Challenge.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Challenge.Infrastructure.Persistence.Repositories;

public class EvaluationCriteriaRepository : IEvaluationCriteriaRepository
{
    private readonly ChallengeDbContext _context;

    public EvaluationCriteriaRepository(ChallengeDbContext context)
    {
        _context = context;
    }

    public async Task<EvaluationCriteria?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.EvaluationCriteria.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<EvaluationCriteria?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalized = name?.Trim() ?? string.Empty;
        return await _context.EvaluationCriteria
            .FirstOrDefaultAsync(x => x.Name.ToLower() == normalized.ToLower(), cancellationToken);
    }

    public async Task<List<EvaluationCriteria>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids?.Distinct().ToList() ?? new List<Guid>();
        if (!idList.Any())
        {
            return new List<EvaluationCriteria>();
        }

        return await _context.EvaluationCriteria
            .Where(x => idList.Contains(x.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(EvaluationCriteria criteria, CancellationToken cancellationToken = default)
    {
        _context.EvaluationCriteria.Add(criteria);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

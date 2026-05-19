using Challenge.Domain.Entities;

namespace Challenge.Domain.Repositories;

public interface IEvaluationCriteriaRepository
{
    Task<EvaluationCriteria?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<EvaluationCriteria?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<List<EvaluationCriteria>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);
    Task AddAsync(EvaluationCriteria criteria, CancellationToken cancellationToken = default);
}

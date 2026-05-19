using Challenge.Domain.Entities;

namespace Challenge.Domain.Repositories;

public interface IChallengeCriteriaRepository
{
    Task<List<ChallengeCriteria>> GetByVersionAsync(Guid challengeVersionId, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<ChallengeCriteria> challengeCriteria, CancellationToken cancellationToken = default);
}

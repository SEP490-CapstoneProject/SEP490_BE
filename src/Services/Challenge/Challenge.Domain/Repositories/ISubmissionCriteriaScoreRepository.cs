using Challenge.Domain.Entities;

namespace Challenge.Domain.Repositories;

public interface ISubmissionCriteriaScoreRepository
{
    Task AddAsync(SubmissionCriteriaScore score, CancellationToken cancellationToken = default);
    Task AddRangeAsync(IEnumerable<SubmissionCriteriaScore> scores, CancellationToken cancellationToken = default);
}

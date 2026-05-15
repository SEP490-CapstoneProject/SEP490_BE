using Challenge.Domain.Entities;

namespace Challenge.Domain.Repositories;

public interface ISubmissionCriteriaScoreRepository
{
    Task AddAsync(SubmissionCriteriaScore score, CancellationToken cancellationToken = default);
}

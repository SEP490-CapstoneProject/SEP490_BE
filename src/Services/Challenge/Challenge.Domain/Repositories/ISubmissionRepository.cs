using Challenge.Domain.Entities;
namespace Challenge.Domain.Repositories;

public interface ISubmissionRepository
{
    Task<ChallengeSubmission> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChallengeSubmission>> GetByChallengeAsync(Guid challengeId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChallengeSubmission>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChallengeSubmission>> GetByUserAndChallengeAsync(Guid userId, Guid challengeId, CancellationToken cancellationToken = default);
    
    Task AddAsync(ChallengeSubmission submission, CancellationToken cancellationToken = default);
    Task UpdateAsync(ChallengeSubmission submission, CancellationToken cancellationToken = default);
    Task<int> GetAttemptCountAsync(Guid userId, Guid challengeId, CancellationToken cancellationToken = default);
}

using Challenge.Domain.Entities;
namespace Challenge.Domain.Repositories;

public interface ISubmissionRepository
{
    Task<ChallengeSubmission> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChallengeSubmission>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<ChallengeSubmission>> GetByChallengeAsync(Guid challengeId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChallengeSubmission>> GetByUserAsync(int userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChallengeSubmission>> GetByUserAndChallengeAsync(int userId, Guid challengeId, CancellationToken cancellationToken = default);
    
    Task AddAsync(ChallengeSubmission submission, CancellationToken cancellationToken = default);
    Task UpdateAsync(ChallengeSubmission submission, CancellationToken cancellationToken = default);
    Task<int> GetAttemptCountAsync(int userId, Guid challengeId, CancellationToken cancellationToken = default);
}

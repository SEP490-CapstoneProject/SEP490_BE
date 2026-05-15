using Challenge.Domain.Entities;
using Challenge.Domain.Enums;
using ChallengeEntity = Challenge.Domain.Entities.Challenge;

namespace Challenge.Domain.Repositories;

public interface IChallengeRepository
{
    Task<ChallengeEntity> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChallengeEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<ChallengeEntity>> GetByStatusAsync(ChallengeStatus status, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChallengeEntity>> GetPublishedAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<ChallengeEntity>> GetExpiredAsync(CancellationToken cancellationToken = default);
    
    Task AddAsync(ChallengeEntity challenge, CancellationToken cancellationToken = default);
    Task UpdateAsync(ChallengeEntity challenge, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}

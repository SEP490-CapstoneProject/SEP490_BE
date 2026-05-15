using Challenge.Domain.Entities;
namespace Challenge.Domain.Repositories;

public interface IChallengeVersionRepository
{
    Task<ChallengeVersion> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ChallengeVersion> GetActiveVersionAsync(Guid challengeId, CancellationToken cancellationToken = default);
    Task<IEnumerable<ChallengeVersion>> GetVersionsByChallengeAsync(Guid challengeId, CancellationToken cancellationToken = default);
    
    Task AddAsync(ChallengeVersion version, CancellationToken cancellationToken = default);
    Task UpdateAsync(ChallengeVersion version, CancellationToken cancellationToken = default);
}

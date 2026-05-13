using Challenge.Domain.Repositories;
using Challenge.Domain.Enums;
namespace Challenge.Application.Services;


public interface IChallengeExpirationJob
{
    Task ExecuteAsync(CancellationToken cancellationToken = default);
}

public class ChallengeExpirationJob : IChallengeExpirationJob
{
    private readonly IChallengeRepository _challengeRepository;
    private readonly ILogger<ChallengeExpirationJob> _logger;

    public ChallengeExpirationJob(
        IChallengeRepository challengeRepository,
        ILogger<ChallengeExpirationJob> logger)
    {
        _challengeRepository = challengeRepository;
        _logger = logger;
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting challenge expiration job");

        var published = await _challengeRepository.GetPublishedAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var expiredCount = 0;

        foreach (var challenge in published)
        {
            if (challenge.Deadline < now && challenge.Status == ChallengeStatus.Published)
            {
                challenge.Status = ChallengeStatus.Expired;
                challenge.UpdatedAt = now;

                await _challengeRepository.UpdateAsync(challenge, cancellationToken);
                expiredCount++;

                _logger.LogInformation("Expired challenge {ChallengeId}: {Title}", challenge.Id, challenge.Title);
            }
        }

        _logger.LogInformation("Challenge expiration job completed. Expired {Count} challenges", expiredCount);
    }
}

using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Challenge.Infrastructure.Services;

/// <summary>
/// Background job service for challenge expiration
/// </summary>
public interface IChallengeExpirationJobService
{
    /// <summary>
    /// Run challenge expiration job to mark expired challenges
    /// </summary>
    Task RunExpirationJobAsync();

    /// <summary>
    /// Check and expire a specific challenge
    /// </summary>
    Task<bool> ExpireChallengeAsync(int challengeId);

    /// <summary>
    /// Get count of expired challenges
    /// </summary>
    Task<int> GetExpiredChallengeCountAsync();
}

/// <summary>
/// Implementation of background job for challenge expiration
/// </summary>
public class ChallengeExpirationJobService : IChallengeExpirationJobService
{
    private readonly ILogger<ChallengeExpirationJobService> _logger;

    public ChallengeExpirationJobService(ILogger<ChallengeExpirationJobService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Run challenge expiration job
    /// Runs daily to mark challenges past their deadline as Expired
    /// </summary>
    public async Task RunExpirationJobAsync()
    {
        try
        {
            _logger.LogInformation("Starting challenge expiration job");

            var now = DateTime.UtcNow;

            // TODO: Query challenges with status "Published" and Deadline < now
            // Mark them as "Expired"
            // Publish ChallengeExpiredEvent

            _logger.LogInformation("Challenge expiration job completed");

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in challenge expiration job");
            throw;
        }
    }

    public async Task<bool> ExpireChallengeAsync(int challengeId)
    {
        try
        {
            _logger.LogInformation($"Expiring challenge {challengeId}");

            // TODO: Update challenge status to "Expired"
            // Publish event

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error expiring challenge {challengeId}");
            return false;
        }
    }

    public async Task<int> GetExpiredChallengeCountAsync()
    {
        try
        {
            // TODO: Query count of challenges with Deadline < now and status != "Expired"
            return 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting expired challenge count");
            return 0;
        }
    }
}

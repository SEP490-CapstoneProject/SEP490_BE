using Challenge.Domain.Entities;

namespace Challenge.Application.Services;

public interface ISkillPointEngine
{
    Task<Dictionary<Guid, decimal>> CalculatePointsAsync(ChallengeVersion version, Dictionary<Guid, decimal> criteriaScores, int attemptCount, CancellationToken cancellationToken = default);
    Task AwardPointsAsync(Guid userId, Dictionary<Guid, decimal> pointsBySkill, Guid submissionId, CancellationToken cancellationToken = default);
    Task RecalculateUserSkillsAsync(Guid userId, CancellationToken cancellationToken = default);
}

public class SkillPointEngine : ISkillPointEngine
{
    public SkillPointEngine(ISkillPointTransactionRepository transactionRepo, IUserSkillRepository userSkillRepo, ILogger<SkillPointEngine> logger)
    {
    }

    public Task<Dictionary<Guid, decimal>> CalculatePointsAsync(ChallengeVersion version, Dictionary<Guid, decimal> criteriaScores, int attemptCount, CancellationToken cancellationToken = default)
        => Task.FromResult(criteriaScores);

    public Task AwardPointsAsync(Guid userId, Dictionary<Guid, decimal> pointsBySkill, Guid submissionId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task RecalculateUserSkillsAsync(Guid userId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

using Challenge.Domain.Entities;

namespace Challenge.Application.Services;

public interface ISubmissionGradingOrchestrator
{
    Task GradeAndAwardAsync(Guid submissionId, CancellationToken cancellationToken = default);
}

public class SubmissionGradingOrchestrator : ISubmissionGradingOrchestrator
{
    public SubmissionGradingOrchestrator(
        ISubmissionRepository submissionRepo,
        IChallengeVersionRepository versionRepo,
        IGeminiAIService aiService,
        ISkillPointEngine pointEngine,
        ISkillNormalizationService skillNormalization,
        ILogger<SubmissionGradingOrchestrator> logger)
    {
    }

    public Task GradeAndAwardAsync(Guid submissionId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

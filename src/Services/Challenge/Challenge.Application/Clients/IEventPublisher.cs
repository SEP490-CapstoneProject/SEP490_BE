namespace Challenge.Application.Clients;

/// <summary>
/// Client for publishing domain events to RabbitMQ
/// </summary>
public interface IEventPublisher
{
    Task PublishChallengeCreatedAsync(int challengeId, string title, int createdById);
    Task PublishChallengePublishedAsync(int challengeId, string title);
    Task PublishSubmissionGradedAsync(int submissionId, int userId, int challengeId, double score);
    Task PublishSkillPointsAwardedAsync(int userId, Dictionary<int, double> skillPoints, int challengeId);
}

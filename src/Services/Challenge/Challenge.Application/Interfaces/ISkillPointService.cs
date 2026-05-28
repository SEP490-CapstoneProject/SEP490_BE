using Challenge.Domain.Entities;

namespace Challenge.Application.Interfaces;

/// <summary>
/// Internal service for calculating and awarding skill points with anti-farming logic
/// </summary>
public interface ISkillPointService
{
    Task<Dictionary<Guid, double>> CalculateSkillPointsAsync(
        ChallengeSubmission submission,
        ChallengeVersion version,
        Dictionary<string, double> criteriaScores);

    Task AwardPointsAsync(
        int userId,
        Dictionary<Guid, double> skillPoints,
        Guid sourceId,
        string reason);

    Task<List<SkillPointTransaction>> GetUserPointTransactionsAsync(int userId);
}

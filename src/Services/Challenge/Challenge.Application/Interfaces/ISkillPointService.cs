using Challenge.Domain.Entities;

namespace Challenge.Application.Interfaces;

/// <summary>
/// Internal service for calculating and awarding skill points with anti-farming logic
/// </summary>
public interface ISkillPointService
{
    Task<Dictionary<int, double>> CalculateSkillPointsAsync(
        ChallengeSubmission submission,
        ChallengeVersion version,
        Dictionary<int, double> criteriaScores);

    Task AwardPointsAsync(
        int userId,
        Dictionary<int, double> skillPoints,
        int challengeId,
        string reason);

    Task<List<SkillPointTransaction>> GetUserPointTransactionsAsync(int userId);
}

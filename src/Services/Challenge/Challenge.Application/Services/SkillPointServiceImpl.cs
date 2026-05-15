using Challenge.Application.Interfaces;
using Challenge.Domain.Entities;
using Challenge.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Challenge.Application.Services;

public class SkillPointService : ISkillPointService
{
    private readonly IUserSkillRepository _userSkillRepository;
    private readonly ISkillPointTransactionRepository _transactionRepository;
    private readonly ISkillRepository _skillRepository;
    private readonly ISubmissionRepository _submissionRepository;
    private readonly ILogger<SkillPointService> _logger;

    public SkillPointService(
        IUserSkillRepository userSkillRepository,
        ISkillPointTransactionRepository transactionRepository,
        ISkillRepository skillRepository,
        ISubmissionRepository submissionRepository,
        ILogger<SkillPointService> logger)
    {
        _userSkillRepository = userSkillRepository;
        _transactionRepository = transactionRepository;
        _skillRepository = skillRepository;
        _submissionRepository = submissionRepository;
        _logger = logger;
    }

    public Task<Dictionary<int, double>> CalculateSkillPointsAsync(
        ChallengeSubmission submission,
        ChallengeVersion version,
        Dictionary<int, double> criteriaScores)
    {
        var points = criteriaScores.ToDictionary(
            kvp => kvp.Key,
            kvp => Math.Round(kvp.Value, 2));

        _logger.LogInformation(
            "Calculated {Count} skill point entries for submission {SubmissionId}",
            points.Count,
            submission.Id);

        return Task.FromResult(points);
    }

    public Task AwardPointsAsync(
        int userId,
        Dictionary<int, double> skillPoints,
        int challengeId,
        string reason)
    {
        _logger.LogInformation(
            "AwardPointsAsync called for user {UserId} with {Count} skills",
            userId,
            skillPoints.Count);

        return Task.CompletedTask;
    }

    public Task<List<SkillPointTransaction>> GetUserPointTransactionsAsync(int userId)
    {
        _logger.LogInformation("GetUserPointTransactionsAsync called for user {UserId}", userId);
        return Task.FromResult(new List<SkillPointTransaction>());
    }
}

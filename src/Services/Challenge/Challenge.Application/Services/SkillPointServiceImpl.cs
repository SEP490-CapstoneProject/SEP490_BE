using Challenge.Application.Interfaces;
using Challenge.Domain.Entities;
using Challenge.Domain.Enums;
using Challenge.Domain.Repositories;
using Microsoft.Extensions.Logging;
using System.Text.Json;

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

    public async Task<Dictionary<Guid, double>> CalculateSkillPointsAsync(
        ChallengeSubmission submission,
        ChallengeVersion version,
        Dictionary<string, double> criteriaScores)
    {
        // Extract score from submission (0-100)
        var submissionScore = Math.Min(100, Math.Max(0, (double)submission.OverallScore));
        
        // Difficulty multiplier: 1x for Easy, 1.5x for Medium, 2x for Hard
        var difficultyMultiplier = version.DifficultyLabel?.ToLower() switch
        {
            "hard" => 2.0,
            "medium" => 1.5,
            _ => 1.0
        };
        
        // Attempt penalty: 1.0 for first, 0.8 for second, 0.6 for third, etc.
        var attemptMultiplier = Math.Max(0.2, 1.0 - ((submission.AttemptCount - 1) * 0.2));
        
        // Parse skill weights from JSON
        var skillWeights = ParseSkillWeights(version.SkillWeightMapping);
        
        // Calculate points for each skill
        var points = new Dictionary<Guid, double>();
        foreach (var skillWeight in skillWeights)
        {
            var skill = await _skillRepository.GetBySlugAsync(NormalizeSlug(skillWeight.Key));
            if (skill is null)
            {
                _logger.LogWarning("Skill '{SkillName}' not found while calculating points for submission {SubmissionId}", skillWeight.Key, submission.Id);
                continue;
            }

            var basePoints = (submissionScore / 100.0) * skillWeight.Value;
            var finalPoints = Math.Round(basePoints * difficultyMultiplier * attemptMultiplier, 2);
            points[skill.Id] = Math.Max(0, finalPoints);
        }

        _logger.LogInformation(
            "Calculated {Count} skill points for submission {SubmissionId}: score={Score}, difficulty={Difficulty}x, attempt={Attempt}x",
            points.Count,
            submission.Id,
            submissionScore,
            difficultyMultiplier,
            attemptMultiplier);

        return await Task.FromResult(points);
    }

    public async Task AwardPointsAsync(
        int userId,
        Dictionary<Guid, double> skillPoints,
        Guid sourceId,
        string reason)
    {
        ArgumentNullException.ThrowIfNull(skillPoints);
        
        var now = DateTime.UtcNow;
        
        try
        {
            foreach (var (skillId, points) in skillPoints.OrderBy(kvp => kvp.Key))
            {
                if (points <= 0)
                {
                    continue;
                }

                var skill = await _skillRepository.GetByIdAsync(skillId);
                if (skill is null)
                {
                    _logger.LogWarning("Skipping awarding points because skill {SkillId} was not found", skillId);
                    continue;
                }

                // Get or create UserSkill
                var existingUserSkill = await _userSkillRepository.GetByUserAndSkillAsync(userId, skill.Id);
                UserSkill userSkill;
                if (existingUserSkill == null)
                {
                    userSkill = new UserSkill
                    {
                        Id = Guid.NewGuid(),
                        UserId = userId,
                        SkillId = skill.Id,
                        TotalPoints = 0,
                        MasteryScore = 0,
                        VerifiedChallengeCount = 0,
                        LastVerifiedAt = null,
                        VerificationLevel = VerificationLevel.Beginner,
                        IsVerified = false,
                        CreatedAt = now,
                        UpdatedAt = now
                    };

                    // Apply points
                    userSkill.TotalPoints += (decimal)points;
                    userSkill.MasteryScore = CalculateMasteryScore(userSkill.TotalPoints);
                    userSkill.VerificationLevel = CalculateVerificationLevel(userSkill.TotalPoints);
                    userSkill.IsVerified = userSkill.VerificationLevel >= VerificationLevel.Intermediate;
                    userSkill.LastVerifiedAt = now;
                    userSkill.VerifiedChallengeCount++;
                    userSkill.UpdatedAt = now;

                    await _userSkillRepository.AddAsync(userSkill);
                }
                else
                {
                    userSkill = existingUserSkill;

                    userSkill.TotalPoints += (decimal)points;
                    userSkill.MasteryScore = CalculateMasteryScore(userSkill.TotalPoints);
                    userSkill.VerificationLevel = CalculateVerificationLevel(userSkill.TotalPoints);
                    userSkill.IsVerified = userSkill.VerificationLevel >= VerificationLevel.Intermediate;
                    userSkill.LastVerifiedAt = now;
                    userSkill.VerifiedChallengeCount++;
                    userSkill.UpdatedAt = now;

                    await _userSkillRepository.UpdateAsync(userSkill);
                }

                // Create transaction record
                var transaction = new SkillPointTransaction
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    SkillId = skill.Id,
                    Points = (decimal)points,
                    SourceType = "ChallengeSubmission",
                    SourceId = sourceId,
                    CreatedAt = now
                };

                await _transactionRepository.AddAsync(transaction);

                _logger.LogInformation(
                    "Awarded {Points} points for skill {SkillId} to user {UserId}. TotalPoints={Total}, Level={Level}",
                    points,
                    skill.Id,
                    userId,
                    userSkill.TotalPoints,
                    userSkill.VerificationLevel);

            }

            _logger.LogInformation(
                "Successfully awarded points to user {UserId} for {Count} skills",
                userId,
                skillPoints.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error awarding points to user {UserId}: {Message}",
                userId,
                ex.Message);
            throw;
        }
    }

    public async Task<List<SkillPointTransaction>> GetUserPointTransactionsAsync(int userId)
    {
        var transactions = await _transactionRepository.GetByUserAsync(userId);
        return transactions.ToList();
    }

    private Dictionary<string, double> ParseSkillWeights(string skillWeightMapping)
    {
        if (string.IsNullOrWhiteSpace(skillWeightMapping))
            return new Dictionary<string, double> { { "Unknown", 1.0 } };

        try
        {
            using var doc = JsonDocument.Parse(skillWeightMapping);
            var weights = new Dictionary<string, double>();
            
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                if (prop.Value.TryGetDouble(out var weight))
                {
                    weights[prop.Name] = weight;
                }
            }

            return weights.Count > 0 ? weights : new Dictionary<string, double> { { "Unknown", 1.0 } };
        }
        catch
        {
            _logger.LogWarning("Failed to parse skill weights: {Mapping}", skillWeightMapping);
            return new Dictionary<string, double> { { "Unknown", 1.0 } };
        }
    }

    private decimal CalculateMasteryScore(decimal totalPoints)
    {
        // Mastery score: 0-100
        // 0-10 points = 10-30 mastery
        // 10-50 points = 30-70 mastery
        // 50+ points = 70-100 mastery
        var mastery = (totalPoints / 50m * 70m) + 30m;
        return Math.Min(100m, mastery);
    }

    private VerificationLevel CalculateVerificationLevel(decimal totalPoints)
    {
        return totalPoints switch
        {
            < 10 => VerificationLevel.Beginner,
            < 50 => VerificationLevel.Intermediate,
            < 100 => VerificationLevel.Advanced,
            _ => VerificationLevel.Expert
        };
    }

    private static string NormalizeSlug(string value)
    {
        return string.Join(
            "-",
            value
                .Trim()
                .ToLowerInvariant()
                .Split(new[] { ' ', '\t', '\r', '\n', '/', '\\', '+', '.', ',', ':', ';', '(', ')' }, StringSplitOptions.RemoveEmptyEntries));
    }
}

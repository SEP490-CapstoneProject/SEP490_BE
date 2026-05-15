using Challenge.Application.Interfaces;
using Challenge.Domain.Entities;
using Challenge.Domain.Enums;
using Challenge.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Challenge.Application.Services;

public class PortfolioSkillsService : IPortfolioSkillsService
{
    private readonly IUserSkillRepository _userSkillRepository;
    private readonly ISkillRepository _skillRepository;
    private readonly ISkillPointTransactionRepository _transactionRepository;
    private readonly ILogger<PortfolioSkillsService> _logger;

    public PortfolioSkillsService(
        IUserSkillRepository userSkillRepository,
        ISkillRepository skillRepository,
        ISkillPointTransactionRepository transactionRepository,
        ILogger<PortfolioSkillsService> logger)
    {
        _userSkillRepository = userSkillRepository;
        _skillRepository = skillRepository;
        _transactionRepository = transactionRepository;
        _logger = logger;
    }

    public async Task<PortfolioSkillsDisplayDto> GetVerifiedSkillsForPortfolioAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var userSkills = (await _userSkillRepository.GetVerifiedByUserAsync(userId, cancellationToken)).ToList();
        var skillNames = await LoadSkillNamesAsync(cancellationToken);

        return new PortfolioSkillsDisplayDto
        {
            UserId = userId,
            TotalVerifiedSkills = userSkills.Count,
            TotalPoints = userSkills.Sum(s => s.TotalPoints),
            AverageMasteryScore = userSkills.Count > 0 ? userSkills.Average(s => s.MasteryScore) : 0,
            TopSkills = userSkills
                .OrderByDescending(s => s.TotalPoints)
                .Take(5)
                .Select(s => new PortfolioSkillDto
                {
                    SkillId = s.SkillId,
                    SkillName = skillNames.TryGetValue(s.SkillId, out var skillName) ? skillName : s.SkillId.ToString(),
                    TotalPoints = s.TotalPoints,
                    VerificationLevel = s.VerificationLevel,
                    MasteryScore = s.MasteryScore,
                    ChallengeCount = s.VerifiedChallengeCount,
                    LastVerifiedAt = s.LastVerifiedAt
                })
                .ToList(),
            VerificationLevelBreakdown = new
            {
                Beginner = userSkills.Count(s => s.VerificationLevel == VerificationLevel.Beginner),
                Intermediate = userSkills.Count(s => s.VerificationLevel == VerificationLevel.Intermediate),
                Advanced = userSkills.Count(s => s.VerificationLevel == VerificationLevel.Advanced),
                Expert = userSkills.Count(s => s.VerificationLevel == VerificationLevel.Expert)
            }
        };
    }

    public async Task<List<UserSkillWithHistoryDto>> GetSkillHistoryAsync(Guid userId, Guid? skillId = null, CancellationToken cancellationToken = default)
    {
        var userSkills = (await _userSkillRepository.GetByUserAsync(userId, cancellationToken)).ToList();
        if (skillId.HasValue)
        {
            userSkills = userSkills.Where(s => s.SkillId == skillId.Value).ToList();
        }

        var skillNames = await LoadSkillNamesAsync(cancellationToken);
        var result = new List<UserSkillWithHistoryDto>();

        foreach (var userSkill in userSkills.OrderByDescending(s => s.TotalPoints))
        {
            var transactions = (await _transactionRepository.GetByUserAndSkillAsync(userId, userSkill.SkillId, cancellationToken))
                .OrderByDescending(t => t.CreatedAt)
                .ToList();

            result.Add(new UserSkillWithHistoryDto
            {
                SkillId = userSkill.SkillId,
                SkillName = skillNames.TryGetValue(userSkill.SkillId, out var skillName) ? skillName : userSkill.SkillId.ToString(),
                TotalPoints = userSkill.TotalPoints,
                MasteryScore = userSkill.MasteryScore,
                VerificationLevel = userSkill.VerificationLevel,
                ChallengeCount = userSkill.VerifiedChallengeCount,
                LastVerifiedAt = userSkill.LastVerifiedAt,
                FirstVerifiedAt = transactions.Count > 0 ? transactions.Min(t => t.CreatedAt) : userSkill.LastVerifiedAt,
                PointTransactions = transactions.Select(t => new PointTransactionDto
                {
                    TransactionId = t.Id,
                    Points = t.Points,
                    Reason = $"{t.SourceType}:{t.SourceId}",
                    AttemptCount = userSkill.VerifiedChallengeCount,
                    SourceType = t.SourceType,
                    SourceId = t.SourceId,
                    CreatedAt = t.CreatedAt
                }).ToList()
            });
        }

        return result;
    }

    public async Task<LeaderboardDto> GetSkillLeaderboardAsync(int limit = 10, string verificationLevel = "Expert", CancellationToken cancellationToken = default)
    {
        var allUserSkills = (await _userSkillRepository.GetAllAsync(cancellationToken)).ToList();
        var filtered = allUserSkills
            .Where(s => string.Equals(s.VerificationLevel.ToString(), verificationLevel, StringComparison.OrdinalIgnoreCase))
            .GroupBy(s => s.UserId)
            .Select(group => new LeaderboardEntryDto
            {
                UserId = group.Key,
                UserName = BuildDisplayName(group.Key),
                TotalPoints = group.Sum(s => s.TotalPoints),
                SkillCount = group.Count(),
                AverageMastery = group.Any() ? group.Average(s => s.MasteryScore) : 0
            })
            .OrderByDescending(x => x.TotalPoints)
            .ThenByDescending(x => x.SkillCount)
            .Take(limit)
            .Select((entry, index) =>
            {
                entry.Rank = index + 1;
                return entry;
            })
            .ToList();

        return new LeaderboardDto
        {
            Title = $"{verificationLevel} Skills Leaderboard",
            VerificationLevel = verificationLevel,
            Entries = filtered,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public async Task<UserSkillStatsDto> GetUserSkillStatisticsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var userSkills = (await _userSkillRepository.GetByUserAsync(userId, cancellationToken)).ToList();
        var verifiedSkills = userSkills.Where(s => s.TotalPoints > 0).ToList();

        return new UserSkillStatsDto
        {
            UserId = userId,
            TotalSkills = userSkills.Count,
            VerifiedSkills = verifiedSkills.Count,
            TotalPoints = verifiedSkills.Sum(s => s.TotalPoints),
            TotalChallenges = verifiedSkills.Sum(s => s.VerifiedChallengeCount),
            AverageMasteryScore = verifiedSkills.Count > 0 ? verifiedSkills.Average(s => s.MasteryScore) : 0,
            HighestMasteryScore = verifiedSkills.Count > 0 ? verifiedSkills.Max(s => s.MasteryScore) : 0,
            MostPointsSkill = verifiedSkills.OrderByDescending(s => s.TotalPoints).FirstOrDefault(),
            LastSkillVerifiedAt = verifiedSkills.Any() ? verifiedSkills.Max(s => s.LastVerifiedAt) : null,
            LevelDistribution = new
            {
                Beginner = verifiedSkills.Count(s => s.VerificationLevel == VerificationLevel.Beginner),
                Intermediate = verifiedSkills.Count(s => s.VerificationLevel == VerificationLevel.Intermediate),
                Advanced = verifiedSkills.Count(s => s.VerificationLevel == VerificationLevel.Advanced),
                Expert = verifiedSkills.Count(s => s.VerificationLevel == VerificationLevel.Expert)
            }
        };
    }

    private async Task<Dictionary<Guid, string>> LoadSkillNamesAsync(CancellationToken cancellationToken)
    {
        var skills = await _skillRepository.GetAllAsync(cancellationToken);
        return skills.ToDictionary(s => s.Id, s => s.Name);
    }

    private static string BuildDisplayName(Guid userId)
        => $"User-{userId.ToString("N")[..8]}";
}

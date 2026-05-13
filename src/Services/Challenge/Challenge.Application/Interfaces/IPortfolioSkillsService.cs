using Challenge.Domain.Enums;

namespace Challenge.Application.Interfaces;

public interface IPortfolioSkillsService
{
    Task<PortfolioSkillsDisplayDto> GetVerifiedSkillsForPortfolioAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<List<UserSkillWithHistoryDto>> GetSkillHistoryAsync(Guid userId, Guid? skillId = null, CancellationToken cancellationToken = default);
    Task<LeaderboardDto> GetSkillLeaderboardAsync(int limit = 10, string verificationLevel = "Expert", CancellationToken cancellationToken = default);
    Task<UserSkillStatsDto> GetUserSkillStatisticsAsync(Guid userId, CancellationToken cancellationToken = default);
}

public class PortfolioSkillsDisplayDto
{
    public Guid UserId { get; set; }
    public int TotalVerifiedSkills { get; set; }
    public decimal TotalPoints { get; set; }
    public decimal AverageMasteryScore { get; set; }
    public List<PortfolioSkillDto> TopSkills { get; set; } = new();
    public object? VerificationLevelBreakdown { get; set; }
}

public class PortfolioSkillDto
{
    public Guid SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public decimal TotalPoints { get; set; }
    public VerificationLevel VerificationLevel { get; set; }
    public decimal MasteryScore { get; set; }
    public int ChallengeCount { get; set; }
    public DateTime? LastVerifiedAt { get; set; }
}

public class UserSkillWithHistoryDto
{
    public Guid SkillId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public decimal TotalPoints { get; set; }
    public decimal MasteryScore { get; set; }
    public VerificationLevel VerificationLevel { get; set; }
    public int ChallengeCount { get; set; }
    public DateTime? LastVerifiedAt { get; set; }
    public DateTime? FirstVerifiedAt { get; set; }
    public List<PointTransactionDto> PointTransactions { get; set; } = new();
}

public class PointTransactionDto
{
    public Guid TransactionId { get; set; }
    public decimal Points { get; set; }
    public string Reason { get; set; } = string.Empty;
    public int AttemptCount { get; set; }
    public string SourceType { get; set; } = string.Empty;
    public Guid SourceId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class LeaderboardDto
{
    public string Title { get; set; } = string.Empty;
    public string VerificationLevel { get; set; } = string.Empty;
    public List<LeaderboardEntryDto> Entries { get; set; } = new();
    public DateTime UpdatedAt { get; set; }
}

public class LeaderboardEntryDto
{
    public int Rank { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public decimal TotalPoints { get; set; }
    public int SkillCount { get; set; }
    public decimal AverageMastery { get; set; }
}

public class UserSkillStatsDto
{
    public Guid UserId { get; set; }
    public int TotalSkills { get; set; }
    public int VerifiedSkills { get; set; }
    public decimal TotalPoints { get; set; }
    public int TotalChallenges { get; set; }
    public decimal AverageMasteryScore { get; set; }
    public decimal HighestMasteryScore { get; set; }
    public Domain.Entities.UserSkill? MostPointsSkill { get; set; }
    public DateTime? LastSkillVerifiedAt { get; set; }
    public object? LevelDistribution { get; set; }
}

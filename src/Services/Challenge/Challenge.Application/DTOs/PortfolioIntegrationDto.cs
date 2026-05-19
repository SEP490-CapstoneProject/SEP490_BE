using Challenge.Domain.Enums;
namespace Challenge.Application.DTOs;


public class VerifiedSkillDto
{
    public Guid SkillId { get; set; }
    public string SkillName { get; set; }
    public decimal TotalPoints { get; set; }
    public decimal MasteryScore { get; set; }
    public int VerifiedChallengeCount { get; set; }
    public VerificationLevel VerificationLevel { get; set; }
    public DateTime LastVerifiedAt { get; set; }
}

public class SkillHistoryItemDto
{
    public Guid TransactionId { get; set; }
    public string ChallengeTitle { get; set; }
    public decimal PointsAwarded { get; set; }
    public DateTime EarnedAt { get; set; }
}

public class LeaderboardEntryDto
{
    public int UserId { get; set; }
    public string UserName { get; set; }
    public decimal TotalPoints { get; set; }
    public int VerifiedChallengeCount { get; set; }
    public int Rank { get; set; }
}

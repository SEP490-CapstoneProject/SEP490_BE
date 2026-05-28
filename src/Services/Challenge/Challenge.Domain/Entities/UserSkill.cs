using Challenge.Domain.Enums;
namespace Challenge.Domain.Entities;

public class UserSkill
{
    public Guid Id { get; set; }

    public int UserId { get; set; }

    public Guid SkillId { get; set; }

    public decimal TotalPoints { get; set; }

    public decimal MasteryScore { get; set; }

    public int VerifiedChallengeCount { get; set; }

    public DateTime? LastVerifiedAt { get; set; }

    public VerificationLevel VerificationLevel { get; set; }

    public bool IsVerified { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

using Challenge.Domain.Enums;
namespace Challenge.Domain.Entities;

public class Challenge
{
    public Guid Id { get; set; }

    public string Title { get; set; }

    public string Description { get; set; }

    public string ExpectedSolution { get; set; }

    public decimal DifficultyScore { get; set; }

    public string DifficultyLabel { get; set; }

    public ChallengeStatus Status { get; set; }

    public Guid? CurrentVersionId { get; set; }

    public int CreatedById { get; set; }

    public int? ReviewedById { get; set; }

    public string RejectionReason { get; set; }

    public DateTime Deadline { get; set; }

    public DateTime? PublishedAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public virtual ChallengeVersion CurrentVersion { get; set; }
}

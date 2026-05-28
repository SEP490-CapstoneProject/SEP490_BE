using Challenge.Domain.Enums;
namespace Challenge.Domain.Entities;

public class ChallengeSubmission
{
    public Guid Id { get; set; }

    public Guid ChallengeId { get; set; }

    public int UserId { get; set; }

    public string SubmissionContent { get; set; }

    public string GithubUrl { get; set; }

    public decimal OverallScore { get; set; }

    public string AiFeedback { get; set; }

    public SubmissionStatus Status { get; set; }

    // Reference to immutable snapshot
    public Guid VersionSnapshotId { get; set; }

    public Guid VersionId { get; set; }

    // Track attempts for anti-farming
    public int AttemptCount { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? GradedAt { get; set; }

    public DateTime UpdatedAt { get; set; }

    // Navigation properties
    public virtual ChallengeVersion Version { get; set; }
}

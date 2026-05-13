namespace Challenge.Application.Models.Events;

/// <summary>
/// Event models for publishing to event bus (RabbitMQ)
/// </summary>

public class ChallengeCreatedEvent
{
    public int ChallengeId { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public int CreatedById { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class ChallengePublishedEvent
{
    public int ChallengeId { get; set; }
    public string Title { get; set; } = "";
    public int VersionId { get; set; }
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
}

public class SubmissionGradedEvent
{
    public int SubmissionId { get; set; }
    public int UserId { get; set; }
    public int ChallengeId { get; set; }
    public double OverallScore { get; set; }
    public Dictionary<string, double> CriteriaScores { get; set; } = new();
    public DateTime GradedAt { get; set; } = DateTime.UtcNow;
}

public class SkillPointsAwardedEvent
{
    public int UserId { get; set; }
    public int SubmissionId { get; set; }
    public int ChallengeId { get; set; }
    public Dictionary<int, double> SkillPoints { get; set; } = new();
    public DateTime AwardedAt { get; set; } = DateTime.UtcNow;
}

public class UserSkillVerificationUpdatedEvent
{
    public int UserId { get; set; }
    public int SkillId { get; set; }
    public string VerificationLevel { get; set; } = "";
    public double TotalPoints { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}

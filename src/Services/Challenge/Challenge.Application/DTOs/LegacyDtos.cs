namespace Challenge.Application.DTOs;

public class ChallengeDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime Deadline { get; set; }
    public DateTime? PublishedAt { get; set; }
}

public class UpdateSkillDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
}

public class UserSkillDto
{
    public Guid UserId { get; set; }
    public Guid SkillId { get; set; }
    public decimal TotalPoints { get; set; }
    public decimal MasteryScore { get; set; }
    public string VerificationLevel { get; set; } = "Beginner";
    public int ChallengeCount { get; set; }
    public DateTime? LastVerifiedAt { get; set; }
}

public class SubmissionDto
{
    public Guid Id { get; set; }
    public Guid ChallengeId { get; set; }
    public Guid UserId { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal OverallScore { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? GradedAt { get; set; }
}

public class SubmitSolutionDto
{
    public string Content { get; set; } = string.Empty;
    public string? GithubUrl { get; set; }
}

public class RejectChallengeDto
{
    public string Reason { get; set; } = string.Empty;
}

using Challenge.Domain.Enums;
namespace Challenge.Application.DTOs;


public class SubmitChallengeDto
{
    public string SubmissionContent { get; set; }
    public string GithubUrl { get; set; }
}

public class SubmissionResponseDto
{
    public Guid Id { get; set; }
    public Guid ChallengeId { get; set; }
    public decimal OverallScore { get; set; }
    public string AiFeedback { get; set; }
    public SubmissionStatus Status { get; set; }
    public int AttemptCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? GradedAt { get; set; }
}

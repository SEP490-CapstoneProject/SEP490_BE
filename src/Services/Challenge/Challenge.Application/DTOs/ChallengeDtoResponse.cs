using Challenge.Domain.Enums;
namespace Challenge.Application.DTOs;


public class ChallengeDtoResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public decimal DifficultyScore { get; set; }
    public string DifficultyLabel { get; set; }
    public ChallengeStatus Status { get; set; }
    public DateTime Deadline { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public string RejectionReason { get; set; }
}

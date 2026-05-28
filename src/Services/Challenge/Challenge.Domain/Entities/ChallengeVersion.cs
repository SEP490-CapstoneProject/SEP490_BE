namespace Challenge.Domain.Entities;

public class ChallengeVersion
{
    public Guid Id { get; set; }

    public Guid ChallengeId { get; set; }

    public int VersionNumber { get; set; }

    public string Title { get; set; }

    public string Description { get; set; }

    public string ExpectedSolution { get; set; }

    public decimal DifficultyScore { get; set; }

    public string DifficultyLabel { get; set; }

    // JSON: { "SignalR": 5, "ASP.NET Core": 3, "C#": 2 }
    public string SkillWeightMapping { get; set; }

    // Gemini, Claude, etc.
    public string ModelName { get; set; }

    // v1.0-sanitized, v2.0, etc.
    public string PromptVersion { get; set; }

    public DateTime EvaluatedAt { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public virtual Challenge Challenge { get; set; }
}

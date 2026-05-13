namespace Challenge.Domain.Entities;

public class PromptSanitizationLog
{
    public Guid Id { get; set; }

    public Guid VersionId { get; set; }

    // JSON: ["injection_attempt", "system_prompt_keyword"]
    public string RiskFlags { get; set; }

    // e.g., "Removed 2 injection markers, truncated to 4800 chars"
    public string SanitizationSummary { get; set; }

    // SHA256 hash of sanitized prompt
    public string PromptHash { get; set; }

    public string ModelName { get; set; }

    public string PromptVersion { get; set; }

    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public virtual ChallengeVersion Version { get; set; }
}

namespace RecruitmentPlatform.AI.Models;

public sealed class MatchingCandidate
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> Skills { get; set; } = new();
    public List<string> Categories { get; set; } = new();
    public float[] Embedding { get; set; } = Array.Empty<float>();
    public string EmbeddingStatus { get; set; } = "Pending";
    public int EmbeddingVersion { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class MatchingRequest
{
    public int SourceId { get; set; }
    public string SourceTitle { get; set; } = string.Empty;
    public string SourceDescription { get; set; } = string.Empty;
    public List<string> SourceSkills { get; set; } = new();
    public List<string> SourceCategories { get; set; } = new();
    public float[] SourceEmbedding { get; set; } = Array.Empty<float>();
    public int SourceEmbeddingVersion { get; set; }
}

public sealed class ScoredMatch
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public double Cosine { get; set; }
    public double SkillScore { get; set; }
    public double CategoryScore { get; set; }
    public double FinalScore { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public sealed class PagedMatchResult
{
    public List<ScoredMatch> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public sealed class MatchingOptions
{
    public int MinimumDescriptionLength { get; set; } = 50;
    public int PreFilterTake { get; set; } = 500;
    public double MinimumFinalScore { get; set; } = 0.3;
    public int MaxPageSize { get; set; } = 50;
}

public sealed class EmbeddingTextInput
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public IReadOnlyCollection<string>? Skills { get; set; }
    public IReadOnlyCollection<string>? Categories { get; set; }
    public IReadOnlyCollection<string>? Projects { get; set; }
    public IReadOnlyCollection<string>? CustomFields { get; set; }
}

public sealed class ModerationResult
{
    public string Status { get; set; } = "Approved";
    public string Reason { get; set; } = string.Empty;
}

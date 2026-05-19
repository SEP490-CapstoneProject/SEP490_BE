namespace Challenge.Application.DTOs;

/// <summary>
/// Challenge data for creator view (list of challenges created by user)
/// </summary>
public class CreatorChallengeDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid? CurrentVersionId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime Deadline { get; set; }
}

/// <summary>
/// Challenge version data for creator view
/// </summary>
public class ChallengeVersionDto
{
    public Guid Id { get; set; }
    public Guid ChallengeId { get; set; }
    public int VersionNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ExpectedSolution { get; set; } = string.Empty;
    public decimal DifficultyScore { get; set; }
    public string DifficultyLabel { get; set; } = string.Empty;
    public string SkillWeightMapping { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string PromptVersion { get; set; } = string.Empty;
    public DateTime EvaluatedAt { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Criteria and skill mappings for this version
    public List<ChallengeVersionCriteriaDto> Criteria { get; set; } = new();
    public List<VersionSkillMappingDto> SkillMappings { get; set; } = new();
}

/// <summary>
/// Criteria for a specific version
/// </summary>
public class ChallengeVersionCriteriaDto
{
    public Guid Id { get; set; }
    public Guid VersionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public decimal MaxScore { get; set; }
    public int DisplayOrder { get; set; }
}

/// <summary>
/// Skill mapping for a version
/// </summary>
public class VersionSkillMappingDto
{
    public Guid Id { get; set; }
    public Guid VersionId { get; set; }
    public string SkillName { get; set; } = string.Empty;
    public decimal Weight { get; set; }
    public List<Guid> CriteriaIds { get; set; } = new();
}

/// <summary>
/// Public challenge data for participants (published challenges only)
/// </summary>
public class PublicChallengeDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal DifficultyScore { get; set; }
    public string DifficultyLabel { get; set; } = string.Empty;
    public DateTime Deadline { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public int CreatedById { get; set; }
    public int? ReviewedById { get; set; }
    
    // Active version details
    public Guid? CurrentVersionId { get; set; }
    public PublicVersionDto? ActiveVersion { get; set; }
}

/// <summary>
/// Version data for participants (skill names + weights, criteria)
/// </summary>
public class PublicVersionDto
{
    public Guid Id { get; set; }
    public int VersionNumber { get; set; }
    public decimal DifficultyScore { get; set; }
    public string DifficultyLabel { get; set; } = string.Empty;
    
    // Skill names and weights (no internal mapping details)
    public Dictionary<string, decimal> SkillWeights { get; set; } = new();
    
    // Criteria visible to participants
    public List<PublicCriteriaDto> Criteria { get; set; } = new();
    
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Public criteria (skill-agnostic)
/// </summary>
public class PublicCriteriaDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal MaxScore { get; set; }
    public int DisplayOrder { get; set; }
}

/// <summary>
/// DTO for updating active version
/// </summary>
public class UpdateActiveVersionDto
{
    public Guid VersionId { get; set; }
}

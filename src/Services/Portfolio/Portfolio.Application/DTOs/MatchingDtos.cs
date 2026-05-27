using RecruitmentPlatform.AI.Models;

namespace Portfolio.Application.DTOs;

public sealed class JobMatchResultDto
{
    // Matching scores (giống match-portfolios)
    public int PostId { get; set; }
    public string Title { get; set; } = string.Empty;
    public double Cosine { get; set; }
    public double SkillScore { get; set; }
    public double CategoryScore { get; set; }
    public double FinalScore { get; set; }

    // Job post detail fields (như API get post)
    public string? CompanyName { get; set; }
    public string? CompanyAvatar { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? MediaType { get; set; }
    public string? MediaUrl { get; set; }
    public string? Address { get; set; }
    public string? Salary { get; set; }
    public string? EmploymentType { get; set; }
    public int? ExperienceYear { get; set; }
    public int? Quantity { get; set; }
    public string? JobDescription { get; set; }
    public string? RequirementsMandatory { get; set; }
    public string? RequirementsPreferred { get; set; }
    public string? Benefits { get; set; }
    public int Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsSaved { get; set; }
    public int? ReviewStatus { get; set; }
    public string? ReviewReason { get; set; }
}

public sealed class JobMatchPagedResult
{
    public List<JobMatchResultDto> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public sealed class MatchingCandidateFeed
{
    public List<MatchingCandidate> Items { get; set; } = new();
}

using RecruitmentPlatform.AI.Models;

namespace Portfolio.Application.DTOs;

public sealed class JobMatchResultDto
{
    public int JobId { get; set; }
    public string Title { get; set; } = string.Empty;
    public double Cosine { get; set; }
    public double SkillScore { get; set; }
    public double CategoryScore { get; set; }
    public double FinalScore { get; set; }

    // Job post detail fields
    public string? Address { get; set; }
    public string? Salary { get; set; }
    public string? EmploymentType { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? CompanyName { get; set; }
    public string? CompanyAvatar { get; set; }
    public int CompanyId { get; set; }
    public DateTime CreatedAt { get; set; }
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

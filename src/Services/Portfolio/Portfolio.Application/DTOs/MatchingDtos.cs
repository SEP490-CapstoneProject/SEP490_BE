using RecruitmentPlatform.AI.Models;

namespace Portfolio.Application.DTOs;

public sealed class JobMatchResultDto
{
    public int PostId { get; set; }
    public string Position { get; set; } = string.Empty;
    public string? CompanyName { get; set; }
    public string? CompanyAvatar { get; set; }
    public string? CoverImageUrl { get; set; }
    public string? MediaType { get; set; }
    public string? MediaUrl { get; set; }
    public string? Address { get; set; }
    public string? Salary { get; set; }
    public string? EmploymentType { get; set; }
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

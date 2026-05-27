using RecruitmentPlatform.AI.Models;

namespace Portfolio.Application.Interfaces;

public interface ICompanyMatchingClient
{
    Task<IReadOnlyList<MatchingCandidate>> GetJobCandidatesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CompanyPostDetailForMatchDto>> GetPostsByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);
}

/// <summary>
/// Lightweight job post detail returned from Company service for match enrichment.
/// </summary>
public class CompanyPostDetailForMatchDto
{
    public int PostId { get; set; }
    public int CompanyId { get; set; }
    public string Position { get; set; } = string.Empty;
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


using RecruitmentPlatform.AI.Models;

namespace Company.Application.Clients;

public interface IPortfolioMatchingClient
{
    Task<IReadOnlyList<MatchingCandidate>> GetPortfolioCandidatesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PortfolioDetailDto>> GetPortfoliosByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default);
}

/// <summary>
/// Lightweight portfolio detail returned from Portfolio service for match enrichment.
/// </summary>
public class PortfolioDetailDto
{
    public int PortfolioId { get; set; }
    public int EmployeeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ModerationStatus { get; set; } = string.Empty;
    public bool IsMain { get; set; }
    public bool IsPublic { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

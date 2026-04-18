namespace Portfolio.Application.DTOs;

public class CreatePortfolioFollowRequest
{
    public int PortfolioId { get; set; }
    public string InterestLevel { get; set; } = string.Empty;
}

public class UpdatePortfolioFollowRequest
{
    public string InterestLevel { get; set; } = string.Empty;
}

public class PortfolioPreviewDto
{
    public int BlockId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Variant { get; set; } = string.Empty;
    public object Data { get; set; } = new();
}

public class PortfolioFollowDto
{
    public int PortfolioId { get; set; }
    public int EmployeeId { get; set; }
    public string PortfolioName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string InterestLevel { get; set; } = string.Empty;
    public DateTime FollowedAt { get; set; }
    public DateTime? LastPortfolioUpdateAt { get; set; }
    public bool IsUpdatedSinceFollow { get; set; }
    public PortfolioPreviewDto? Preview { get; set; }
}

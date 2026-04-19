namespace Portfolio.Application.DTOs;

public class CreatePortfolioFollowRequest
{
    public int PortfolioId { get; set; }
    public string InterestLevel { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
}

public class UpdatePortfolioFollowRequest
{
    public string InterestLevel { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
}

public class PortfolioPreviewDto
{
    public int Id { get; set; }
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
    public int? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public string? CategoryCode { get; set; }
    public DateTime FollowedAt { get; set; }
    public DateTime? LastPortfolioUpdateAt { get; set; }
    public bool IsUpdatedSinceFollow { get; set; }
    public PortfolioPreviewDto? Preview { get; set; }
}

public class CreateFollowCategoryRequest
{
    public string Name { get; set; } = string.Empty;
}

public class UpdateFollowCategoryRequest
{
    public string Name { get; set; } = string.Empty;
}

public class FollowCategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

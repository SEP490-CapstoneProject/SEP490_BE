using Portfolio.Domain.Entities;

namespace Portfolio.Application.DTOs;

public class ComplimentDto
{
    public int Id { get; set; }
    public int PortfolioId { get; set; }
    public int CompanyId { get; set; }
    public string Content { get; set; } = string.Empty;
    public int? Score { get; set; }
    public ComplimentState State { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateComplimentRequest
{
    public int PortfolioId { get; set; }
    public string Content { get; set; } = string.Empty;
    public int? Score { get; set; }
}

public class UpdateComplimentRequest
{
    public string Content { get; set; } = string.Empty;
    public int? Score { get; set; }
}

public class PatchComplimentStateRequest
{
    public ComplimentState State { get; set; }
}

public class PortfolioWithComplimentDto
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int ComplimentCount { get; set; }
    public int ApprovedComplimentCount { get; set; }
    public decimal? AverageScore { get; set; }
    public List<ComplimentDto>? Compliments { get; set; }
}

public class PortfolioQueryParams
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Status { get; set; }
    public bool IncludeCompliments { get; set; } = false;
    public ComplimentState? ComplimentState { get; set; }
    public bool? HasCompliment { get; set; }
}

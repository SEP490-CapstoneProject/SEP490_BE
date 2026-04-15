using Portfolio.Domain.Entities;

namespace Portfolio.Application.DTOs;

public class ComplimentDto
{
    public int Id { get; set; }
    public int PortfolioId { get; set; }
    public int UserId { get; set; }
    public string? Content { get; set; }
    public int? Score { get; set; }
    public ComplimentState State { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateComplimentRequest
{
    public int PortfolioId { get; set; }
    public string? Content { get; set; }
    public int? Score { get; set; }
}

public class UpdateComplimentRequest
{
    public string? Content { get; set; }
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
    public bool IsMain { get; set; }
    public bool IsPublic { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int ComplimentCount { get; set; }
    public int ApprovedComplimentCount { get; set; }
    public decimal? AverageScore { get; set; }
    public RankingDto Ranking { get; set; } = new();
    public List<BlockDto> Blocks { get; set; } = new();
    public List<PortfolioReviewerDto> Reviewers { get; set; } = new();
    public List<ComplimentDto>? Compliments { get; set; }
}

public class RankingDto
{
    public decimal TotalScore { get; set; }
    public decimal AverageScore { get; set; }
    public int RankPosition { get; set; }
}

public class PortfolioReviewerDto
{
    public int UserId { get; set; }
    public string? Name { get; set; }
    public string? Avatar { get; set; }
    public string? Role { get; set; }
}

public class PortfolioQueryParams
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? Status { get; set; }
    public bool IncludeCompliments { get; set; } = false;
    public ComplimentState? ComplimentState { get; set; }
    public bool? HasCompliment { get; set; }
    public PortfolioRankBy RankBy { get; set; } = PortfolioRankBy.average;
    public PortfolioSortMode Sort { get; set; } = PortfolioSortMode.newest;
}

public enum PortfolioRankBy
{
    average = 0,
    total = 1
}

public enum PortfolioSortMode
{
    rank_asc = 0,
    rank_desc = 1,
    random = 2,
    newest = 3,
    oldest = 4
}

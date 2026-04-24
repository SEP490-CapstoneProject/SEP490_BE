using RecruitmentPlatform.AI.Models;

namespace Company.Application.DTOs;

public sealed class PortfolioMatchResultDto
{
    public int PortfolioId { get; set; }
    public string Title { get; set; } = string.Empty;
    public double Cosine { get; set; }
    public double SkillScore { get; set; }
    public double CategoryScore { get; set; }
    public double FinalScore { get; set; }
}

public sealed class PortfolioMatchPagedResult
{
    public List<PortfolioMatchResultDto> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public sealed class MatchingCandidateFeed
{
    public List<MatchingCandidate> Items { get; set; } = new();
}

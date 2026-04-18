namespace Portfolio.Domain.Entities;

public class PortfolioFollow
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public int PortfolioId { get; set; }
    public int? CategoryId { get; set; }
    public string InterestLevel { get; set; } = "medium";
    public DateTime FollowedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public Portfolio Portfolio { get; set; } = null!;
    public PortfolioFollowCategory? Category { get; set; }
}

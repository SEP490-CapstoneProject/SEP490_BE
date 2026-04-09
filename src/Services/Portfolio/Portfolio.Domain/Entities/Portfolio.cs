namespace Portfolio.Domain.Entities;

public class Portfolio
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "active";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Aggregate fields (recalculated from DB, never modified directly)
    public int ComplimentCount { get; set; } = 0;
    public int ApprovedComplimentCount { get; set; } = 0;
    public decimal? AverageScore { get; set; }

    public ICollection<PortfolioBlock> Blocks { get; set; } = new List<PortfolioBlock>();
    public ICollection<Compliment> Compliments { get; set; } = new List<Compliment>();
    public ICollection<PortfolioFollow> Follows { get; set; } = new List<PortfolioFollow>();
}

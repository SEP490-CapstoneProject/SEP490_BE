namespace Portfolio.Domain.Entities;

public class PortfolioFollowCategory
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<PortfolioFollow> Follows { get; set; } = new List<PortfolioFollow>();
}

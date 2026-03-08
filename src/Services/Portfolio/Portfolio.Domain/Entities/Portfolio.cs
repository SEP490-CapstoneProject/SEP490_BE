namespace Portfolio.Domain.Entities;

public class Portfolio
{
    public int Id { get; set; }
    public int EmployeeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "active";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<PortfolioBlock> Blocks { get; set; } = new List<PortfolioBlock>();
}

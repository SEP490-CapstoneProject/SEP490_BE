namespace Portfolio.Domain.Entities;

public class Activities
{
    public int Id { get; set; }
    public int PortfolioBlockId { get; set; }
    public string? Name { get; set; }
    public DateOnly? Date { get; set; }
    public string? Description { get; set; }

    public PortfolioBlock PortfolioBlock { get; set; } = null!;
}

namespace Portfolio.Domain.Entities;

public class Reference
{
    public int Id { get; set; }
    public int PortfolioBlockId { get; set; }
    public string? Name { get; set; }
    public string? Position { get; set; }
    public string? Mail { get; set; }
    public string? Phone { get; set; }

    public PortfolioBlock PortfolioBlock { get; set; } = null!;
}

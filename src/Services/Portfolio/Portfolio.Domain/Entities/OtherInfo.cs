namespace Portfolio.Domain.Entities;

public class OtherInfo
{
    public int Id { get; set; }
    public int PortfolioBlockId { get; set; }
    public string? Detail { get; set; }

    public PortfolioBlock PortfolioBlock { get; set; } = null!;
}

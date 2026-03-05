namespace Portfolio.Domain.Entities;

public class Skill
{
    public int Id { get; set; }
    public int PortfolioBlockId { get; set; }
    public string Name { get; set; } = string.Empty;

    public PortfolioBlock PortfolioBlock { get; set; } = null!;
}

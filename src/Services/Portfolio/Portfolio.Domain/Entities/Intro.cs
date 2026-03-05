namespace Portfolio.Domain.Entities;

public class Intro
{
    public int Id { get; set; }
    public int PortfolioBlockId { get; set; }
    public string? Avatar { get; set; }
    public string? Name { get; set; }
    public string? StudyField { get; set; }
    public string? Description { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }

    public PortfolioBlock PortfolioBlock { get; set; } = null!;
}

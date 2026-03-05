namespace Portfolio.Domain.Entities;

public class Experience
{
    public int Id { get; set; }
    public int PortfolioBlockId { get; set; }
    public string? JobName { get; set; }
    public string? Address { get; set; }
    public string? StartDate { get; set; }
    public string? EndDate { get; set; }
    public string? Description { get; set; }

    public PortfolioBlock PortfolioBlock { get; set; } = null!;
}

namespace Portfolio.Domain.Entities;

public class Education
{
    public int Id { get; set; }
    public int PortfolioBlockId { get; set; }
    public string? SchoolName { get; set; }
    public string? Time { get; set; }
    public string? Department { get; set; }
    public string? Description { get; set; }

    public PortfolioBlock PortfolioBlock { get; set; } = null!;
}

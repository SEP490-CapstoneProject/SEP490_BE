namespace Portfolio.Domain.Entities;

public class Project
{
    public int Id { get; set; }
    public int PortfolioBlockId { get; set; }
    public string? Image { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Role { get; set; }
    public string? Technology { get; set; }

    public PortfolioBlock PortfolioBlock { get; set; } = null!;
    public ICollection<ProjectLink> Links { get; set; } = new List<ProjectLink>();
}

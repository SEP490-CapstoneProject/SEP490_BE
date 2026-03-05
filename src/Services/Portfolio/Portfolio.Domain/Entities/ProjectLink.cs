namespace Portfolio.Domain.Entities;

public class ProjectLink
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string? Type { get; set; }
    public string? Link { get; set; }

    public Project Project { get; set; } = null!;
}

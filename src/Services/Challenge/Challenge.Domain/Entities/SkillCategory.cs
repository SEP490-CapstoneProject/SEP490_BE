namespace Challenge.Domain.Entities;

public class SkillCategory
{
    public Guid Id { get; set; }

    public string Name { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }
}

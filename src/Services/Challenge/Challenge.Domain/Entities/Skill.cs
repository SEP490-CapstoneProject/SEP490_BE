namespace Challenge.Domain.Entities;

public class Skill
{
    public Guid Id { get; set; }

    public string Name { get; set; }

    public string Slug { get; set; }

    public Guid CategoryId { get; set; }

    public bool IsSystem { get; set; }

    public bool IsApproved { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

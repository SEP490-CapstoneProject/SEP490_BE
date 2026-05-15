namespace Challenge.Domain.Entities;

public class SkillAlias
{
    public Guid Id { get; set; }

    public Guid SkillId { get; set; }

    public string Alias { get; set; }

    public DateTime CreatedAt { get; set; }
}

using Challenge.Domain.Enums;
namespace Challenge.Domain.Entities;

public class SkillRelationship
{
    public Guid Id { get; set; }

    public Guid SourceSkillId { get; set; }

    public Guid TargetSkillId { get; set; }

    public SkillRelationType RelationType { get; set; }

    // Strength (0-1): relationship strength, not AI confidence
    public decimal Strength { get; set; }

    public DateTime CreatedAt { get; set; }
}

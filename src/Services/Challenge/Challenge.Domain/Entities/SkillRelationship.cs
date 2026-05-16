using Challenge.Domain.Enums;
namespace Challenge.Domain.Entities;

public class SkillRelationship
{
    public Guid Id { get; set; }

    public Guid SourceSkillId { get; set; }

    public Guid TargetSkillId { get; set; }

    public SkillRelationType RelationType { get; set; }

    public decimal Weight { get; set; }

    public DateTime CreatedAt { get; set; }
}

namespace Challenge.Domain.Entities;

public class CriteriaSkillMapping
{
    public Guid Id { get; set; }

    public Guid CriteriaId { get; set; }

    public Guid SkillId { get; set; }

    public decimal Weight { get; set; }

    public DateTime CreatedAt { get; set; }
}

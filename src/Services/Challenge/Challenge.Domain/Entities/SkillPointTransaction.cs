namespace Challenge.Domain.Entities;

public class SkillPointTransaction
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public Guid SkillId { get; set; }

    public decimal Points { get; set; }

    public string SourceType { get; set; }

    public Guid SourceId { get; set; }

    public DateTime CreatedAt { get; set; }
}

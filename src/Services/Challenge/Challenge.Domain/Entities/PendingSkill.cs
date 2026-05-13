using Challenge.Domain.Enums;
namespace Challenge.Domain.Entities;

public class PendingSkill
{
    public Guid Id { get; set; }

    public string ProposedName { get; set; }

    public Guid ProposedById { get; set; }

    public SkillApprovalStatus Status { get; set; }

    public string RejectionReason { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}

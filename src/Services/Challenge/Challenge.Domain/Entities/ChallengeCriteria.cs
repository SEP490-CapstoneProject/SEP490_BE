namespace Challenge.Domain.Entities;

public class ChallengeCriteria
{
    public Guid Id { get; set; }

    public Guid ChallengeVersionId { get; set; }

    public Guid CriteriaId { get; set; }

    public decimal Weight { get; set; }

    public DateTime VersionedAt { get; set; }

    // Navigation properties
    public virtual EvaluationCriteria Criteria { get; set; }
}

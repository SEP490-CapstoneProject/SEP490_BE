namespace Challenge.Domain.Entities;

public class SubmissionCriteriaScore
{
    public Guid Id { get; set; }

    public Guid SubmissionId { get; set; }

    public Guid CriteriaId { get; set; }

    public decimal Score { get; set; }

    public string Feedback { get; set; }

    // AI metadata for traceability
    public string ModelName { get; set; }

    public DateTime GradedAt { get; set; }
}

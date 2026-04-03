namespace Payment.Domain.Entities;

public class PaymentHistory
{
    public Guid Id { get; set; }
    public Guid PaymentId { get; set; }
    public string? OldStatus { get; set; }
    public string NewStatus { get; set; } = null!;
    public string Action { get; set; } = null!;
    public string? RawData { get; set; }
    public string? CorrelationId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public PaymentEntity Payment { get; set; } = null!;
}

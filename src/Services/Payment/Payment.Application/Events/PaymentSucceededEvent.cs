namespace Payment.Application.Events;

public class PaymentSucceededEvent
{
    public string EventType { get; set; } = "payment.succeeded";
    public string EventId { get; set; } = null!;
    public Guid PaymentId { get; set; }
    public int UserId { get; set; }
    public int PlanId { get; set; }
    public decimal Amount { get; set; }
    public string Provider { get; set; } = null!;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

using Payment.Domain.Enums;

namespace Payment.Domain.Entities;

public class PaymentEntity
{
    public Guid Id { get; set; }
    public int UserId { get; set; }
    public int PlanId { get; set; }
    public int SubscriptionId { get; set; }  // Links to Subscription Service
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "VND";
    public PaymentProvider Provider { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? PaymentUrl { get; set; }
    public string? TransactionId { get; set; }
    public string OrderCode { get; set; } = null!;
    public int RowVersion { get; set; } = 0;
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? PaidAt { get; set; }

    public ICollection<PaymentHistory> History { get; set; } = new List<PaymentHistory>();
}

namespace Subscription.Application.Events;

public abstract class BaseEvent
{
    public int Version { get; set; } = 1;
    public string EventId { get; set; } = Guid.NewGuid().ToString();
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
    public string EventType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class PaymentSucceededEvent : BaseEvent
{
    public int SubscriptionId { get; set; }
    public int UserId { get; set; }
    public int PlanId { get; set; }
    public string PaymentId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;

    public PaymentSucceededEvent()
    {
        EventType = "payment.succeeded";
    }
}

public class SubscriptionActivatedEvent : BaseEvent
{
    public int UserId { get; set; }
    public int SubscriptionId { get; set; }
    public int PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public int? PreviousPlanId { get; set; }
    public Dictionary<string, object> Features { get; set; } = new();
    public DateTime StartDate { get; set; }
    public DateTime ExpiredAt { get; set; }

    public SubscriptionActivatedEvent()
    {
        EventType = "subscription.activated";
    }
}

public class SubscriptionUpgradedEvent : BaseEvent
{
    public int UserId { get; set; }
    public int SubscriptionId { get; set; }
    public int OldPlanId { get; set; }
    public int NewPlanId { get; set; }
    public string NewPlanName { get; set; } = string.Empty;
    public Dictionary<string, object> Features { get; set; } = new();
    public DateTime ExpiredAt { get; set; }

    public SubscriptionUpgradedEvent()
    {
        EventType = "subscription.upgraded";
    }
}

public class SubscriptionExpiredEvent : BaseEvent
{
    public int UserId { get; set; }
    public int SubscriptionId { get; set; }
    public string Reason { get; set; } = "expired";

    public SubscriptionExpiredEvent()
    {
        EventType = "subscription.expired";
    }
}

public class SubscriptionCancelledEvent : BaseEvent
{
    public int UserId { get; set; }
    public int SubscriptionId { get; set; }
    public string Reason { get; set; } = string.Empty;

    public SubscriptionCancelledEvent()
    {
        EventType = "subscription.cancelled";
    }
}

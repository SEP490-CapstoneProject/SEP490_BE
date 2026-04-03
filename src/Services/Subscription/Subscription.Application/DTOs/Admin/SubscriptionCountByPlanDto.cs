namespace Subscription.Application.DTOs.Admin;

public class SubscriptionCountByPlanDto
{
    public int PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public int ActiveCount { get; set; }
    public int ExpiredCount { get; set; }
    public int CancelledCount { get; set; }
    public int TotalCount { get; set; }
}

using System.ComponentModel.DataAnnotations;

namespace Subscription.Application.DTOs;

public class SubscribeRequest
{
    [Required]
    public int PlanId { get; set; }
    
    public bool AutoRenew { get; set; } = true;
}

public class UpgradeRequest
{
    [Required]
    public int NewPlanId { get; set; }
}

public class CancelRequest
{
    public string? Reason { get; set; }
}

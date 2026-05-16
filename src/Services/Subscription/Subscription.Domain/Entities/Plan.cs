using Subscription.Domain.Enums;

namespace Subscription.Domain.Entities;

public class Plan
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public BillingCycle BillingCycle { get; set; }
    public bool IsActive { get; set; }
    /// <summary>
    /// Role được phép mua plan này. Null = tất cả role đều mua được.
    /// Ví dụ: "JobSeeker", "Employer", null (all roles)
    /// </summary>
    public string? AllowedRole { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation property
    public ICollection<PlanFeature> Features { get; set; } = new List<PlanFeature>();
    public ICollection<UserSubscription> Subscriptions { get; set; } = new List<UserSubscription>();
}

using Subscription.Domain.Enums;

namespace Subscription.Domain.Entities;

public class PlanFeature
{
    public int Id { get; set; }
    public int PlanId { get; set; }
    public string FeatureKey { get; set; } = string.Empty;
    public string FeatureName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public FeatureType Type { get; set; }
    public bool IsActive { get; set; }

    // Navigation property
    public Plan Plan { get; set; } = null!;
}

namespace Subscription.Application.DTOs;

/// <summary>
/// Feature entitlement details
/// </summary>
public class FeatureEntitlementDto
{
    public string FeatureKey { get; set; } = null!; // "MAX_APPLY", "AI_MATCHING", etc.
    public string FeatureName { get; set; } = null!; // "Lượt ứng tuyển tối đa"
    public string Value { get; set; } = null!; // "5", "true", "-1", etc.
    public string Type { get; set; } = null!; // "0" (Text), "1" (Boolean), "2" (Number), "3" (Text variant)
    public bool IsActive { get; set; }
}

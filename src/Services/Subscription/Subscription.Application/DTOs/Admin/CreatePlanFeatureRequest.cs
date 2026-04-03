namespace Subscription.Application.DTOs.Admin;

public class CreatePlanFeatureRequest
{
    public string FeatureKey { get; set; } = string.Empty;
    public string FeatureName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int Type { get; set; }  // 1=Boolean, 2=Number, 3=Text
}

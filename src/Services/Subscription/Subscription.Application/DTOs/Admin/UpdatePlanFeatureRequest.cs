namespace Subscription.Application.DTOs.Admin;

public class UpdatePlanFeatureRequest
{
    public string? FeatureName { get; set; }
    public string? Value { get; set; }
    public int? Type { get; set; }
}

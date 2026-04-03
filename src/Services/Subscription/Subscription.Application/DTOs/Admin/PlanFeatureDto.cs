namespace Subscription.Application.DTOs.Admin;

public class PlanFeatureDto
{
    public int Id { get; set; }
    public int PlanId { get; set; }
    public string FeatureKey { get; set; } = string.Empty;
    public string FeatureName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

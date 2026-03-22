namespace Subscription.Application.DTOs;

public class PlanDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string BillingCycle { get; set; } = string.Empty;
    public List<PlanFeatureDto> Features { get; set; } = new();
}

public class PlanFeatureDto
{
    public string FeatureKey { get; set; } = string.Empty;
    public string FeatureName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

namespace Portfolio.Application.DTOs;

public class FeatureEntitlementDto
{
    public string FeatureKey { get; set; } = string.Empty;
    public string FeatureName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

public class EntitlementsDto
{
    public int Version { get; set; }
    public int PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public Dictionary<string, object> Features { get; set; } = new();
    public DateTime ExpiredAt { get; set; }
}

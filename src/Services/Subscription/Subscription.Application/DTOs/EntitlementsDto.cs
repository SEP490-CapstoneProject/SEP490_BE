namespace Subscription.Application.DTOs;

public class EntitlementsDto
{
    public int Version { get; set; } = 1;
    public int PlanId { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public Dictionary<string, object> Features { get; set; } = new();
    public DateTime ExpiredAt { get; set; }
}

public class UsageDto
{
    public string FeatureKey { get; set; } = string.Empty;
    public int CurrentUsage { get; set; }
    public int Limit { get; set; }
    public bool IsUnlimited => Limit == -1;
}

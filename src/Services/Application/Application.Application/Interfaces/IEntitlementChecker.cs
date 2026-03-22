namespace Application.Application.Interfaces;

public interface IEntitlementChecker
{
    Task<EntitlementResult> CheckFeatureAsync(int userId, string featureKey);
    Task<(bool success, int currentUsage)> TryIncrementUsageAsync(int userId, string featureKey);
    Task RollbackUsageAsync(int userId, string featureKey);
}

public class EntitlementResult
{
    public bool HasFeature { get; set; }
    public object? FeatureValue { get; set; }
    public int? CurrentUsage { get; set; }
    public int? Limit { get; set; }
    public string? Message { get; set; }
}

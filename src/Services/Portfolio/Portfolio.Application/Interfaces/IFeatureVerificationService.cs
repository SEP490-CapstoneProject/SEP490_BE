using Portfolio.Application.DTOs;

namespace Portfolio.Application.Interfaces;

public interface IFeatureVerificationService
{
    /// <summary>
    /// Check if user has access to a specific feature.
    /// </summary>
    Task<bool> HasFeatureAccessAsync(int userId, string featureKey);

    /// <summary>
    /// Get the value/limit of a feature for the user.
    /// Returns: "5", "20", "-1", "true", "false", or null if user doesn't have feature.
    /// </summary>
    Task<string?> GetFeatureValueAsync(int userId, string featureKey);

    /// <summary>
    /// Check if user can perform an action with a limit.
    /// Example: CanPerformActionAsync(userId, "MAX_PORTFOLIOS", currentCount: 3)
    /// </summary>
    Task<bool> CanPerformActionAsync(int userId, string actionKey, int currentCount = 0);

    /// <summary>
    /// Get full feature entitlement details for user.
    /// </summary>
    Task<FeatureEntitlementDto?> GetFeatureEntitlementAsync(int userId, string featureKey);

    /// <summary>
    /// Get all active features/entitlements for user.
    /// Returns dictionary: FeatureKey -> FeatureValue.
    /// </summary>
    Task<Dictionary<string, string>> GetUserEntitlementsAsync(int userId);
}

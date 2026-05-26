using Subscription.Application.DTOs;

namespace Subscription.Application.Interfaces;

/// <summary>
/// Service for verifying user access to subscription plan features
/// </summary>
public interface IFeatureVerificationService
{
    /// <summary>
    /// Check if user has access to a specific feature
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="featureKey">Feature key (e.g., "MAX_APPLY", "AI_MATCHING")</param>
    /// <returns>true if user has access, false otherwise</returns>
    Task<bool> HasFeatureAccessAsync(int userId, string featureKey);

    /// <summary>
    /// Get the value/limit of a feature for the user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="featureKey">Feature key</param>
    /// <returns>Feature value as string ("5", "20", "-1", "true", "false"), or null if user doesn't have feature</returns>
    Task<string?> GetFeatureValueAsync(int userId, string featureKey);

    /// <summary>
    /// Check if user can perform an action with a limit
    /// Used for validating actions like job applications (MAX_APPLY) or portfolio creation (MAX_PORTFOLIOS)
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="actionKey">Action/feature key to check limit for</param>
    /// <param name="currentCount">Current count of performed actions</param>
    /// <returns>true if currentCount < limit (or limit is -1 for unlimited), false otherwise</returns>
    Task<bool> CanPerformActionAsync(int userId, string actionKey, int currentCount = 0);

    /// <summary>
    /// Get full feature entitlement details for user
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <param name="featureKey">Feature key</param>
    /// <returns>Feature entitlement details, or null if user doesn't have feature</returns>
    Task<FeatureEntitlementDto?> GetFeatureEntitlementAsync(int userId, string featureKey);

    /// <summary>
    /// Get all active features/entitlements for user
    /// Results are cached for 1 hour
    /// </summary>
    /// <param name="userId">User ID</param>
    /// <returns>Dictionary mapping feature keys to their values</returns>
    Task<Dictionary<string, string>> GetUserEntitlementsAsync(int userId);
}

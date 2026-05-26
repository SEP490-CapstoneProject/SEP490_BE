using Microsoft.Extensions.Logging;
using Subscription.Application.DTOs;
using Subscription.Application.Interfaces;
using Subscription.Domain.Entities;
using Subscription.Domain.Enums;

namespace Subscription.Application.Services;

public class FeatureVerificationService : IFeatureVerificationService
{
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IPlanRepository _planRepository;
    private readonly IRedisService _redisService;
    private readonly ILogger<FeatureVerificationService> _logger;

    private const string ENTITLEMENTS_CACHE_KEY_PREFIX = "feature_entitlements:{0}"; // {userId}
    private const int ENTITLEMENTS_CACHE_TTL_HOURS = 1;

    public FeatureVerificationService(
        ISubscriptionRepository subscriptionRepository,
        IPlanRepository planRepository,
        IRedisService redisService,
        ILogger<FeatureVerificationService> logger)
    {
        _subscriptionRepository = subscriptionRepository;
        _planRepository = planRepository;
        _redisService = redisService;
        _logger = logger;
    }

    /// <summary>
    /// Check if user has access to a specific feature
    /// </summary>
    public async Task<bool> HasFeatureAccessAsync(int userId, string featureKey)
    {
        try
        {
            var value = await GetFeatureValueAsync(userId, featureKey);
            return !string.IsNullOrEmpty(value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking feature access for user {UserId}, feature {FeatureKey}", 
                userId, featureKey);
            return false;
        }
    }

    /// <summary>
    /// Get the value/limit of a feature for the user
    /// Returns: "5", "20", "-1", "true", "false", or null if user doesn't have feature
    /// </summary>
    public async Task<string?> GetFeatureValueAsync(int userId, string featureKey)
    {
        try
        {
            // Get user entitlements (cached)
            var entitlements = await GetUserEntitlementsAsync(userId);
            
            if (entitlements.TryGetValue(featureKey, out var value))
            {
                return value;
            }

            _logger.LogDebug("Feature {FeatureKey} not found for user {UserId}", featureKey, userId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting feature value for user {UserId}, feature {FeatureKey}", 
                userId, featureKey);
            return null;
        }
    }

    /// <summary>
    /// Check if user can perform an action with a limit
    /// Returns true if currentCount < limit (or limit is -1 for unlimited)
    /// </summary>
    public async Task<bool> CanPerformActionAsync(int userId, string actionKey, int currentCount = 0)
    {
        try
        {
            var featureValue = await GetFeatureValueAsync(userId, actionKey);
            
            if (string.IsNullOrEmpty(featureValue))
            {
                _logger.LogWarning("User {UserId} doesn't have feature {ActionKey}", userId, actionKey);
                return false;
            }

            // Handle unlimited (-1)
            if (featureValue == "-1")
            {
                return true;
            }

            // Try to parse as integer
            if (int.TryParse(featureValue, out var limit))
            {
                var canPerform = currentCount < limit;
                _logger.LogDebug("Feature {ActionKey} check for user {UserId}: current={CurrentCount}, limit={Limit}, allowed={CanPerform}",
                    actionKey, userId, currentCount, limit, canPerform);
                return canPerform;
            }

            _logger.LogWarning("Invalid numeric value for feature {ActionKey}: {Value}", actionKey, featureValue);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking action permission for user {UserId}, action {ActionKey}",
                userId, actionKey);
            return false;
        }
    }

    /// <summary>
    /// Get full feature entitlement details for user
    /// </summary>
    public async Task<FeatureEntitlementDto?> GetFeatureEntitlementAsync(int userId, string featureKey)
    {
        try
        {
            var subscription = await GetUserActiveSubscriptionAsync(userId);
            if (subscription?.Plan == null)
            {
                _logger.LogDebug("No active subscription for user {UserId}, checking free plan", userId);
                return null;
            }

            var plan = subscription.Plan;
            var feature = plan.Features?.FirstOrDefault(f => f.FeatureKey == featureKey && f.IsActive);
            
            if (feature == null)
            {
                _logger.LogDebug("Feature {FeatureKey} not found in user {UserId}'s plan {PlanId}",
                    featureKey, userId, plan.Id);
                return null;
            }

            return MapToFeatureEntitlementDto(feature);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting feature entitlement for user {UserId}, feature {FeatureKey}",
                userId, featureKey);
            return null;
        }
    }

    /// <summary>
    /// Get all active features/entitlements for user
    /// Results are cached for 1 hour using Redis
    /// </summary>
    public async Task<Dictionary<string, string>> GetUserEntitlementsAsync(int userId)
    {
        try
        {
            // Level 1: Check Redis cache using EntitlementsDto
            var cached = await _redisService.GetEntitlementsAsync(userId);
            if (cached != null)
            {
                _logger.LogDebug("Entitlements cache hit for user {UserId}", userId);
                // Convert cached Features (Dict<string, object>) to Dict<string, string>
                return ConvertFeaturesObjectToDictionary(cached.Features);
            }

            // Level 2: Query database
            var subscription = await GetUserActiveSubscriptionAsync(userId);
            Dictionary<string, string> entitlements;

            if (subscription?.Plan != null)
            {
                entitlements = BuildFeatureDictionary(subscription.Plan.Features);
                _logger.LogDebug("Loaded entitlements from DB for user {UserId}, plan {PlanId}",
                    userId, subscription.PlanId);
                
                // Cache using EntitlementsDto format
                var ttl = (subscription.EndDate - DateTime.UtcNow).Add(TimeSpan.FromDays(1));
                var cacheDto = new EntitlementsDto
                {
                    Version = 1,
                    PlanId = subscription.PlanId,
                    PlanName = subscription.Plan.Name,
                    Features = ConvertFeaturesDictionaryToObject(entitlements),
                    ExpiredAt = subscription.EndDate
                };
                await _redisService.SetEntitlementsAsync(userId, cacheDto, ttl);
            }
            else
            {
                // Level 3: No active subscription
                entitlements = new Dictionary<string, string>();
                _logger.LogDebug("No active subscription for user {UserId}, using empty entitlements", userId);
            }

            return entitlements;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting entitlements for user {UserId}", userId);
            // Return empty dict on error instead of throwing
            return new Dictionary<string, string>();
        }
    }

    /// <summary>
    /// Get user's active subscription with plan and features loaded
    /// </summary>
    private async Task<UserSubscription?> GetUserActiveSubscriptionAsync(int userId)
    {
        try
        {
            var subscription = await _subscriptionRepository.GetActiveByUserIdAsync(userId);
            
            // Make sure plan and features are loaded
            if (subscription?.Plan != null && subscription.Plan.Features == null)
            {
                // Reload with features if needed
                subscription.Plan = await _planRepository.GetByIdWithFeaturesAsync(subscription.PlanId) 
                    ?? subscription.Plan;
            }

            return subscription;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active subscription for user {UserId}", userId);
            return null;
        }
    }

    /// <summary>
    /// Build a dictionary of feature key -> value from plan features
    /// </summary>
    private static Dictionary<string, string> BuildFeatureDictionary(ICollection<PlanFeature>? features)
    {
        var dict = new Dictionary<string, string>();
        
        if (features == null)
        {
            return dict;
        }

        foreach (var feature in features.Where(f => f.IsActive))
        {
            // Store all values as strings for consistency
            dict[feature.FeatureKey] = feature.Value;
        }

        return dict;
    }

    /// <summary>
    /// Convert feature dictionary (string values) to object values for caching
    /// </summary>
    private static Dictionary<string, object> ConvertFeaturesDictionaryToObject(Dictionary<string, string> features)
    {
        var dict = new Dictionary<string, object>();
        foreach (var kvp in features)
        {
            dict[kvp.Key] = kvp.Value;
        }
        return dict;
    }

    /// <summary>
    /// Convert cached features (object values) back to string values
    /// </summary>
    private static Dictionary<string, string> ConvertFeaturesObjectToDictionary(Dictionary<string, object> features)
    {
        var dict = new Dictionary<string, string>();
        foreach (var kvp in features)
        {
            dict[kvp.Key] = kvp.Value?.ToString() ?? string.Empty;
        }
        return dict;
    }

    /// <summary>
    /// Map PlanFeature to FeatureEntitlementDto
    /// </summary>
    private static FeatureEntitlementDto MapToFeatureEntitlementDto(PlanFeature feature)
    {
        return new FeatureEntitlementDto
        {
            FeatureKey = feature.FeatureKey,
            FeatureName = feature.FeatureName,
            Value = feature.Value,
            Type = MapFeatureTypeToString(feature.Type),
            IsActive = feature.IsActive
        };
    }

    /// <summary>
    /// Convert FeatureType enum to string representation
    /// </summary>
    private static string MapFeatureTypeToString(FeatureType type)
    {
        return type switch
        {
            FeatureType.Boolean => "1",
            FeatureType.Number => "2",
            FeatureType.Text => "3",
            _ => "0" // Default to Text
        };
    }

    /// <summary>
    /// Invalidate user's entitlements cache (call after subscription changes)
    /// </summary>
    public async Task InvalidateUserEntitlementsCacheAsync(int userId)
    {
        try
        {
            await _redisService.RemoveEntitlementsAsync(userId);
            _logger.LogInformation("Entitlements cache invalidated for user {UserId}", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error invalidating entitlements cache for user {UserId}", userId);
        }
    }
}


using System.Text.Json;
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

    public async Task<bool> HasFeatureAccessAsync(int userId, string featureKey)
    {
        var value = await GetFeatureValueAsync(userId, featureKey);
        if (value == null)
        {
            return false;
        }

        return !bool.TryParse(value, out var boolValue) || boolValue;
    }

    public async Task<string?> GetFeatureValueAsync(int userId, string featureKey)
    {
        var entitlements = await GetUserEntitlementsAsync(userId);
        return entitlements.TryGetValue(featureKey, out var value) ? value : null;
    }

    public async Task<bool> CanPerformActionAsync(int userId, string actionKey, int currentCount = 0)
    {
        var value = await GetFeatureValueAsync(userId, actionKey);
        if (value == null)
        {
            return false;
        }

        if (value == "-1")
        {
            return true;
        }

        if (!int.TryParse(value, out var limit))
        {
            _logger.LogWarning("Invalid numeric value '{Value}' for feature {FeatureKey}", value, actionKey);
            return false;
        }

        return currentCount < limit;
    }

    public async Task<FeatureEntitlementDto?> GetFeatureEntitlementAsync(int userId, string featureKey)
    {
        var features = await GetPlanFeaturesForUserAsync(userId);
        var feature = features.FirstOrDefault(f => f.IsActive && f.FeatureKey == featureKey);

        if (feature == null)
        {
            return null;
        }

        return new FeatureEntitlementDto
        {
            FeatureKey = feature.FeatureKey,
            FeatureName = feature.FeatureName,
            Value = feature.Value,
            Type = feature.Type.ToString(),
            IsActive = feature.IsActive
        };
    }

    public async Task<Dictionary<string, string>> GetUserEntitlementsAsync(int userId)
    {
        var entitlements = await GetEntitlementsAsync(userId);
        return ConvertToStringDictionary(entitlements.Features);
    }

    private async Task<EntitlementsDto> GetEntitlementsAsync(int userId)
    {
        var cached = await _redisService.GetEntitlementsAsync(userId);
        if (cached != null)
        {
            return cached;
        }

        var subscription = await GetActiveSubscriptionAsync(userId);
        if (subscription != null)
        {
            var entitlements = BuildEntitlements(subscription);
            await _redisService.SetEntitlementsAsync(userId, entitlements);
            return entitlements;
        }

        var freePlan = await GetFreePlanAsync();
        if (freePlan != null)
        {
            var entitlements = BuildEntitlements(freePlan);
            await _redisService.SetEntitlementsAsync(userId, entitlements);
            return entitlements;
        }

        _logger.LogWarning("No free plan found for user {UserId}. Returning empty entitlements.", userId);
        return new EntitlementsDto
        {
            Version = 1,
            PlanId = 0,
            PlanName = "None",
            Features = new Dictionary<string, object>(),
            ExpiredAt = DateTime.UtcNow.AddYears(10)
        };
    }

    private async Task<UserSubscription?> GetActiveSubscriptionAsync(int userId)
    {
        var subscription = await _subscriptionRepository.GetActiveByUserIdAsync(userId);
        if (subscription == null)
        {
            return null;
        }

        if (subscription.Status != SubscriptionStatus.Active || subscription.EndDate <= DateTime.UtcNow)
        {
            _logger.LogWarning("Subscription {SubscriptionId} for user {UserId} is not active or expired.", subscription.Id, userId);
            return null;
        }

        return subscription;
    }

    private async Task<List<PlanFeature>> GetPlanFeaturesForUserAsync(int userId)
    {
        var subscription = await GetActiveSubscriptionAsync(userId);
        if (subscription?.Plan?.Features != null)
        {
            return subscription.Plan.Features.Where(f => f.IsActive).ToList();
        }

        var freePlan = await GetFreePlanAsync();
        return freePlan?.Features.Where(f => f.IsActive).ToList() ?? new List<PlanFeature>();
    }

    private async Task<Plan?> GetFreePlanAsync()
    {
        var plans = await _planRepository.GetAllActiveAsync();
        return plans
            .Where(p => p.Price == 0)
            .OrderBy(p => p.Id)
            .FirstOrDefault();
    }

    private static EntitlementsDto BuildEntitlements(UserSubscription subscription)
    {
        return new EntitlementsDto
        {
            Version = 1,
            PlanId = subscription.PlanId,
            PlanName = subscription.Plan?.Name ?? "Unknown",
            Features = BuildFeaturesDictionary(subscription.Plan?.Features),
            ExpiredAt = subscription.EndDate
        };
    }

    private static EntitlementsDto BuildEntitlements(Plan plan)
    {
        return new EntitlementsDto
        {
            Version = 1,
            PlanId = plan.Id,
            PlanName = plan.Name,
            Features = BuildFeaturesDictionary(plan.Features),
            ExpiredAt = DateTime.UtcNow.AddYears(10)
        };
    }

    private static Dictionary<string, object> BuildFeaturesDictionary(ICollection<PlanFeature>? features)
    {
        var dict = new Dictionary<string, object>();
        if (features == null)
        {
            return dict;
        }

        foreach (var feature in features.Where(f => f.IsActive))
        {
            object value = feature.Type switch
            {
                FeatureType.Boolean => bool.Parse(feature.Value),
                FeatureType.Number => int.Parse(feature.Value),
                _ => feature.Value
            };

            dict[feature.FeatureKey] = value;
        }

        return dict;
    }

    private static Dictionary<string, string> ConvertToStringDictionary(Dictionary<string, object> features)
    {
        var result = new Dictionary<string, string>();
        foreach (var (key, value) in features)
        {
            result[key] = NormalizeValue(value);
        }

        return result;
    }

    private static string NormalizeValue(object value)
    {
        return value switch
        {
            null => string.Empty,
            bool boolValue => boolValue.ToString().ToLowerInvariant(),
            int intValue => intValue.ToString(),
            long longValue => longValue.ToString(),
            decimal decimalValue => decimalValue.ToString(),
            JsonElement jsonElement => JsonElementToString(jsonElement),
            _ => value.ToString() ?? string.Empty
        };
    }

    private static string JsonElementToString(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Number when element.TryGetInt64(out var intValue) => intValue.ToString(),
            JsonValueKind.Number when element.TryGetDecimal(out var decimalValue) => decimalValue.ToString(),
            _ => element.ToString()
        };
    }
}

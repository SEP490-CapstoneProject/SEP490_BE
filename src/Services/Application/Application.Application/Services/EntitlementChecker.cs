using System.Text.Json;
using Application.Application.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Application.Application.Services;

public class EntitlementChecker : IEntitlementChecker
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ISubscriptionClient _subscriptionClient;
    private readonly ILogger<EntitlementChecker> _logger;
    
    // Lua script for atomic quota check
    private const string LuaScript = @"
        local key = KEYS[1]
        local limit = tonumber(ARGV[1])
        if limit == -1 then return redis.call('INCR', key) end
        local current = redis.call('INCR', key)
        if current > limit then
            redis.call('DECR', key)
            return -1
        end
        return current
    ";

    private static readonly EntitlementsDto DefaultFreeTier = new()
    {
        Version = 1,
        PlanId = 1,
        PlanName = "Free",
        Features = new Dictionary<string, object>
        {
            { "MAX_APPLY", 5 },
            { "MAX_PORTFOLIOS", 1 },
            { "AI_MATCHING", false }
        },
        ExpiredAt = DateTime.UtcNow.AddYears(10)
    };

    public EntitlementChecker(
        IConnectionMultiplexer redis,
        ISubscriptionClient subscriptionClient,
        ILogger<EntitlementChecker> logger)
    {
        _redis = redis;
        _subscriptionClient = subscriptionClient;
        _logger = logger;
    }

    public async Task<EntitlementResult> CheckFeatureAsync(int userId, string featureKey)
    {
        var entitlements = await GetEntitlementsWithFallbackAsync(userId);
        
        if (!entitlements.Features.TryGetValue(featureKey, out var featureValue))
        {
            return new EntitlementResult
            {
                HasFeature = false,
                Message = $"Feature '{featureKey}' not available in your plan"
            };
        }

        // Boolean feature
        if (featureValue is bool boolValue)
        {
            return new EntitlementResult
            {
                HasFeature = boolValue,
                FeatureValue = boolValue,
                Message = boolValue ? null : $"Feature '{featureKey}' is not enabled in your plan"
            };
        }

        // Number feature (quota)
        if (featureValue is int limit || (featureValue is JsonElement je && je.TryGetInt32(out limit)))
        {
            var currentUsage = await GetCurrentUsageAsync(userId, featureKey);
            return new EntitlementResult
            {
                HasFeature = limit == -1 || currentUsage < limit,
                FeatureValue = limit,
                CurrentUsage = currentUsage,
                Limit = limit == -1 ? null : limit,
                Message = (limit != -1 && currentUsage >= limit) 
                    ? $"Quota exceeded for '{featureKey}'. Limit: {limit}, Used: {currentUsage}"
                    : null
            };
        }

        return new EntitlementResult
        {
            HasFeature = true,
            FeatureValue = featureValue
        };
    }

    public async Task<(bool success, int currentUsage)> TryIncrementUsageAsync(int userId, string featureKey)
    {
        var entitlements = await GetEntitlementsWithFallbackAsync(userId);
        
        if (!entitlements.Features.TryGetValue(featureKey, out var featureValue))
        {
            _logger.LogWarning("Feature {FeatureKey} not found for user {UserId}", featureKey, userId);
            return (false, 0);
        }

        int limit;
        if (featureValue is int intLimit)
        {
            limit = intLimit;
        }
        else if (featureValue is JsonElement je && je.TryGetInt32(out int jeLimit))
        {
            limit = jeLimit;
        }
        else
        {
            _logger.LogWarning("Feature {FeatureKey} is not a number type", featureKey);
            return (false, 0);
        }

        try
        {
            var db = _redis.GetDatabase();
            var currentMonth = DateTime.UtcNow.ToString("yyyy-MM");
            var usageKey = $"user:{userId}:usage:{featureKey}:{currentMonth}";

            var result = await db.ScriptEvaluateAsync(
                LuaScript,
                new RedisKey[] { usageKey },
                new RedisValue[] { limit });

            var newUsage = (int)result;
            
            if (newUsage == -1)
            {
                _logger.LogWarning("Quota exceeded for user {UserId}, feature {FeatureKey}, limit {Limit}", 
                    userId, featureKey, limit);
                return (false, limit);
            }

            _logger.LogInformation("Usage incremented for user {UserId}, feature {FeatureKey}: {Usage}/{Limit}", 
                userId, featureKey, newUsage, limit);
            return (true, newUsage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to increment usage for user {UserId}, feature {FeatureKey}", userId, featureKey);
            return (false, 0);
        }
    }

    public async Task RollbackUsageAsync(int userId, string featureKey)
    {
        try
        {
            var db = _redis.GetDatabase();
            var currentMonth = DateTime.UtcNow.ToString("yyyy-MM");
            var usageKey = $"user:{userId}:usage:{featureKey}:{currentMonth}";
            
            await db.StringDecrementAsync(usageKey);
            _logger.LogInformation("Usage rolled back for user {UserId}, feature {FeatureKey}", userId, featureKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rollback usage for user {UserId}, feature {FeatureKey}", userId, featureKey);
        }
    }

    private async Task<int> GetCurrentUsageAsync(int userId, string featureKey)
    {
        try
        {
            var db = _redis.GetDatabase();
            var currentMonth = DateTime.UtcNow.ToString("yyyy-MM");
            var usageKey = $"user:{userId}:usage:{featureKey}:{currentMonth}";
            
            var value = await db.StringGetAsync(usageKey);
            return value.HasValue ? (int)value : 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get usage for user {UserId}, feature {FeatureKey}", userId, featureKey);
            return 0;
        }
    }

    private async Task<EntitlementsDto> GetEntitlementsWithFallbackAsync(int userId)
    {
        // Level 1: Redis cache
        try
        {
            var db = _redis.GetDatabase();
            var cached = await db.StringGetAsync($"user:{userId}:entitlements");
            
            if (cached.HasValue)
            {
                var entitlements = JsonSerializer.Deserialize<EntitlementsDto>(cached!, 
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                
                if (entitlements != null && 
                    entitlements.Version == 1 && 
                    entitlements.ExpiredAt > DateTime.UtcNow)
                {
                    return entitlements;
                }
                
                _logger.LogWarning("Stale entitlements for user {UserId}, falling back", userId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis failed for user {UserId}, falling back", userId);
        }

        // Level 2: HTTP call to Subscription Service
        try
        {
            var entitlements = await _subscriptionClient.GetEntitlementsAsync(userId);
            if (entitlements != null)
            {
                // Cache for next time
                try
                {
                    var db = _redis.GetDatabase();
                    var json = JsonSerializer.Serialize(entitlements);
                    await db.StringSetAsync(
                        $"user:{userId}:entitlements", 
                        json, 
                        TimeSpan.FromMinutes(5));
                }
                catch { /* Ignore cache write failure */ }
                
                return entitlements;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Subscription Service call failed for user {UserId}", userId);
        }

        // Level 3: Default free tier
        _logger.LogWarning("Using default free tier for user {UserId}", userId);
        return DefaultFreeTier;
    }
}

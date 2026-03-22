using System.Text.Json;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using Subscription.Application.DTOs;
using Subscription.Application.Interfaces;

namespace Subscription.Application.Services;

public class RedisService : IRedisService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<RedisService> _logger;
    private readonly IDatabase _db;

    // Lua script for atomic check-and-increment (prevents race condition)
    private const string CheckAndIncrementScript = @"
        local key = KEYS[1]
        local limit = tonumber(ARGV[1])
        if limit == -1 then
            -- Unlimited: just increment without checking
            local current = redis.call('INCR', key)
            return current
        end
        local current = redis.call('INCR', key)
        if current > limit then
            redis.call('DECR', key)
            return -1
        end
        return current
    ";

    public RedisService(IConnectionMultiplexer redis, ILogger<RedisService> logger)
    {
        _redis = redis;
        _logger = logger;
        _db = redis.GetDatabase();
    }

    // Entitlements
    public async Task SetEntitlementsAsync(int userId, EntitlementsDto entitlements, TimeSpan? ttl = null)
    {
        var key = $"user:{userId}:entitlements";
        var json = JsonSerializer.Serialize(entitlements);
        
        // Default TTL: subscription expiration + 1 day buffer
        var expiry = ttl ?? (entitlements.ExpiredAt - DateTime.UtcNow).Add(TimeSpan.FromDays(1));
        
        await _db.StringSetAsync(key, json, expiry);
        _logger.LogInformation("Set entitlements for user {UserId}, TTL: {TTL}", userId, expiry);
    }

    public async Task<EntitlementsDto?> GetEntitlementsAsync(int userId)
    {
        var key = $"user:{userId}:entitlements";
        var json = await _db.StringGetAsync(key);
        
        if (json.IsNullOrEmpty)
        {
            _logger.LogDebug("Cache miss for user {UserId} entitlements", userId);
            return null;
        }

        try
        {
            var entitlements = JsonSerializer.Deserialize<EntitlementsDto>(json!);
            
            // Stale data detection
            if (entitlements != null && entitlements.ExpiredAt < DateTime.UtcNow)
            {
                _logger.LogWarning("Stale entitlements detected for user {UserId}, invalidating", userId);
                await RemoveEntitlementsAsync(userId);
                return null;
            }
            
            // Version validation
            if (entitlements != null && entitlements.Version != 1)
            {
                _logger.LogWarning("Entitlements version mismatch for user {UserId}, version: {Version}", 
                    userId, entitlements.Version);
                await RemoveEntitlementsAsync(userId);
                return null;
            }
            
            return entitlements;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to deserialize entitlements for user {UserId}, invalidating", userId);
            await RemoveEntitlementsAsync(userId);
            return null;
        }
    }

    public async Task RemoveEntitlementsAsync(int userId)
    {
        var key = $"user:{userId}:entitlements";
        await _db.KeyDeleteAsync(key);
        _logger.LogInformation("Removed entitlements for user {UserId}", userId);
    }

    public async Task InvalidateUserCacheAsync(int userId)
    {
        var keys = new[]
        {
            $"user:{userId}:entitlements",
            $"user:{userId}:last_event_time"
        };
        
        // Also delete usage keys for current month
        var currentMonth = DateTime.UtcNow.ToString("yyyy-MM");
        var usagePattern = $"user:{userId}:usage:*:{currentMonth}";
        
        foreach (var key in keys)
        {
            await _db.KeyDeleteAsync(key);
        }
        
        _logger.LogInformation("Invalidated all cache for user {UserId}", userId);
    }

    // Usage counters with atomic Lua script
    public async Task<(bool success, int currentUsage)> TryIncrementUsageAsync(int userId, string featureKey, int limit)
    {
        // Use month-based key for quota reset strategy
        var currentMonth = DateTime.UtcNow.ToString("yyyy-MM");
        var key = $"user:{userId}:usage:{featureKey}:{currentMonth}";
        
        try
        {
            var result = await _db.ScriptEvaluateAsync(
                CheckAndIncrementScript,
                new RedisKey[] { key },
                new RedisValue[] { limit }
            );
            
            var currentUsage = (int)result;
            
            if (currentUsage == -1)
            {
                _logger.LogWarning("Quota exceeded for user {UserId}, feature {Feature}, limit {Limit}", 
                    userId, featureKey, limit);
                return (false, limit);
            }
            
            _logger.LogDebug("Usage incremented for user {UserId}, feature {Feature}: {Current}/{Limit}", 
                userId, featureKey, currentUsage, limit);
            return (true, currentUsage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to increment usage for user {UserId}, feature {Feature}", userId, featureKey);
            throw;
        }
    }

    public async Task DecrementUsageAsync(int userId, string featureKey)
    {
        var currentMonth = DateTime.UtcNow.ToString("yyyy-MM");
        var key = $"user:{userId}:usage:{featureKey}:{currentMonth}";
        
        var result = await _db.StringDecrementAsync(key);
        if (result < 0)
        {
            await _db.StringSetAsync(key, 0);
        }
        
        _logger.LogDebug("Usage decremented for user {UserId}, feature {Feature}", userId, featureKey);
    }

    public async Task<int> GetUsageAsync(int userId, string featureKey)
    {
        var currentMonth = DateTime.UtcNow.ToString("yyyy-MM");
        var key = $"user:{userId}:usage:{featureKey}:{currentMonth}";
        
        var value = await _db.StringGetAsync(key);
        return value.IsNullOrEmpty ? 0 : (int)value;
    }

    public async Task ResetUsageAsync(int userId, string featureKey)
    {
        var currentMonth = DateTime.UtcNow.ToString("yyyy-MM");
        var key = $"user:{userId}:usage:{featureKey}:{currentMonth}";
        
        await _db.KeyDeleteAsync(key);
        _logger.LogInformation("Reset usage for user {UserId}, feature {Feature}", userId, featureKey);
    }

    // Last event tracking for out-of-order detection
    public async Task SetLastEventTimeAsync(int userId, DateTime timestamp)
    {
        var key = $"user:{userId}:last_event_time";
        await _db.StringSetAsync(key, timestamp.ToString("O"), TimeSpan.FromDays(30));
    }

    public async Task<DateTime?> GetLastEventTimeAsync(int userId)
    {
        var key = $"user:{userId}:last_event_time";
        var value = await _db.StringGetAsync(key);
        
        if (value.IsNullOrEmpty)
            return null;
            
        return DateTime.Parse(value!);
    }

    // Health check
    public async Task<bool> PingAsync()
    {
        try
        {
            var result = await _db.PingAsync();
            return result.TotalMilliseconds < 5000;
        }
        catch
        {
            return false;
        }
    }
}

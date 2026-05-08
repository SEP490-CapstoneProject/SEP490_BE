using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Notification.Application.Services;

public class FavoriteAggregationService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly TimeSpan _aggregationWindow;
    private readonly TimeSpan _cacheTtl;
    private readonly ILogger<FavoriteAggregationService> _logger;

    public FavoriteAggregationService(IConnectionMultiplexer redis, IConfiguration configuration, ILogger<FavoriteAggregationService> logger)
    {
        _redis = redis;
        _logger = logger;
        
        // Sliding window: prefer seconds (new) over minutes (legacy)
        // Each event resets the TTL, so window adapts to event frequency
        // Example: 15s window means notification sent 15s after last event
        var windowSeconds = configuration.GetValue<int?>("FavoriteAggregation:WindowSeconds");
        if (windowSeconds.HasValue)
        {
            _aggregationWindow = TimeSpan.FromSeconds(windowSeconds.Value);
        }
        else
        {
            // Fallback to minutes for backward compatibility
            var windowMinutes = configuration.GetValue<int?>("FavoriteAggregation:WindowMinutes") ?? 3;
            _aggregationWindow = TimeSpan.FromMinutes(windowMinutes);
        }
        
        // Keep key alive slightly longer than aggregation window so flush service
        // has time to read and process it on next poll cycle.
        _cacheTtl = _aggregationWindow + TimeSpan.FromSeconds(90);
    }

    public async Task<bool> TryAggregateAsync(int postId, string ownerId, string actorId, string actorName, CancellationToken cancellationToken = default)
    {
        var key = $"favorite_agg:{postId}:{ownerId}";
        var db = _redis.GetDatabase();
        
        // Read from Redis using raw Redis (consistent with flush service)
        var cached = await db.StringGetAsync(key);

        if (cached.HasValue)
        {
            // Within window - increment count
            var json = cached.ToString();
            var data = JsonSerializer.Deserialize<AggregationData>(json);
            if (data != null)
            {
                data.Count++;
                data.LastAt = GetVietnamTime();
                data.FirstAt = GetVietnamTime();  // Sliding window: reset on each event

                var jsonData = JsonSerializer.Serialize(data);
                _logger.LogInformation("🔔 [FAV_PRESYNC] CacheHit=true, Key={Key}, DataLength={DataLength}, TTL={TTL}", 
                    key, jsonData.Length, _cacheTtl);

                try
                {
                    await db.StringSetAsync(key, jsonData, _cacheTtl);

                    _logger.LogInformation("🔔 [FAV_POSTSYNC] CacheHit=true persisted, Key={Key}", key);
                }
                catch (Exception ex)
                {
                    _logger.LogError("🔔 [FAV_SYNC_ERROR] CacheHit=true failed, Key={Key}, Error={Error}", key, ex.Message);
                    throw;
                }

                _logger.LogInformation("🔔 [FAV_AGG] CacheHit=true, PostId={PostId}, Count={Count}", postId, data.Count);
                return true; // Aggregated, don't create notification yet
            }
        }

        // New window or expired - store first event
        var newData = new AggregationData
        {
            Count = 1,
            PostId = postId,
            OwnerId = ownerId,
            FirstActorId = actorId,
            FirstActorName = actorName,
            FirstAt = GetVietnamTime(),
            LastAt = GetVietnamTime()
        };

        var newJsonData = JsonSerializer.Serialize(newData);
        _logger.LogInformation("🔔 [FAV_PRESYNC] FirstEvent, Key={Key}, DataLength={DataLength}, TTL={TTL}, ActorName={ActorName}", 
            key, newJsonData.Length, _cacheTtl, actorName);

        try
        {
            await db.StringSetAsync(key, newJsonData, _cacheTtl);

            _logger.LogInformation("🔔 [FAV_POSTSYNC] FirstEvent persisted, Key={Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError("🔔 [FAV_SYNC_ERROR] FirstEvent failed, Key={Key}, Error={Error}", key, ex.Message);
            throw;
        }

        _logger.LogInformation("🔔 [FAV_AGG] CacheHit=false, FirstEvent, PostId={PostId}, ActorName={ActorName}", postId, actorName);
        return false; // First event, create notification immediately
    }

    public async Task<List<AggregationData>> GetExpiredAggregationsAsync(CancellationToken cancellationToken = default)
    {
        // This method would need Redis SCAN functionality
        // For now, we'll use a simplified approach where the background service
        // checks known keys or uses a separate tracking mechanism
        return new List<AggregationData>();
    }

    public async Task RemoveAggregationAsync(int postId, string ownerId, CancellationToken cancellationToken = default)
    {
        var key = $"favorite_agg:{postId}:{ownerId}";
        var db = _redis.GetDatabase();
        await db.KeyDeleteAsync(key);
    }

    private static DateTime GetVietnamTime()
    {
        var utcNow = DateTime.UtcNow;
        var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        return TimeZoneInfo.ConvertTimeFromUtc(utcNow, vietnamTimeZone);
    }
}

public class AggregationData
{
    public int Count { get; set; }
    public int PostId { get; set; }
    public string OwnerId { get; set; } = string.Empty;
    public string FirstActorId { get; set; } = string.Empty;
    public string FirstActorName { get; set; } = string.Empty;
    public DateTime FirstAt { get; set; }
    public DateTime LastAt { get; set; }
}

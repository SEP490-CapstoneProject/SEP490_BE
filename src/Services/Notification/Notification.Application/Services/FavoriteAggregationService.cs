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
        
        var cached = await db.StringGetAsync(key);

        if (cached.HasValue)
        {
            var json = cached.ToString();
            var data = JsonSerializer.Deserialize<AggregationData>(json);
            if (data != null)
            {
                data.Count++;
                data.LastAt = GetVietnamTime();
                data.FirstAt = GetVietnamTime();

                var jsonData = JsonSerializer.Serialize(data);

                try
                {
                    await db.StringSetAsync(key, jsonData, _cacheTtl);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "🔔 [FAV_SYNC_ERROR] CacheHit=true failed, Key={Key}", key);
                    throw;
                }

                return true;
            }
        }

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

        try
        {
            await db.StringSetAsync(key, newJsonData, _cacheTtl);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔔 [FAV_SYNC_ERROR] FirstEvent failed, Key={Key}", key);
            throw;
        }

        return false;
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

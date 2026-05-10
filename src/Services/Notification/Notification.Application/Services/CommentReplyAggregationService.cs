using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Notification.Application.Services;

public class CommentReplyAggregationService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly TimeSpan _aggregationWindow;
    private readonly TimeSpan _cacheTtl;
    private readonly ILogger<CommentReplyAggregationService> _logger;

    public CommentReplyAggregationService(IConnectionMultiplexer redis, IConfiguration configuration, ILogger<CommentReplyAggregationService> logger)
    {
        _redis = redis;
        _logger = logger;
        // Sliding window: prefer seconds (new) over minutes (legacy)
        var windowSeconds = configuration.GetValue<int?>("CommentReplyAggregation:WindowSeconds");
        if (windowSeconds.HasValue)
        {
            _aggregationWindow = TimeSpan.FromSeconds(windowSeconds.Value);
        }
        else
        {
            var windowMinutes = configuration.GetValue<int?>("CommentReplyAggregation:WindowMinutes") ?? 3;
            _aggregationWindow = TimeSpan.FromMinutes(windowMinutes);
        }
        _cacheTtl = _aggregationWindow + TimeSpan.FromSeconds(90);
    }

    public async Task<bool> TryAggregateAsync(
        string eventType,
        string objectId,
        string ownerId,
        string actorId,
        string actorName,
        CancellationToken cancellationToken = default)
    {
        var key = BuildAggregationKey(eventType, objectId, ownerId);
        var db = _redis.GetDatabase();
        
        // Read from Redis using raw Redis (consistent with flush service)
        var cached = await db.StringGetAsync(key);

        if (cached.HasValue)
        {
            var json = cached.ToString();
            var data = JsonSerializer.Deserialize<CommentReplyAggregationData>(json);
            if (data != null)
            {
                data.Count++;
                data.LastAt = GetVietnamTime();
                data.FirstAt = GetVietnamTime();  // Sliding window: reset on each event

                var jsonData = JsonSerializer.Serialize(data);
                _logger.LogInformation("💬 [COM_PRESYNC] CacheHit=true, Key={Key}, DataLength={DataLength}, TTL={TTL}", 
                    key, jsonData.Length, _cacheTtl);

                try
                {
                    await db.StringSetAsync(key, jsonData, _cacheTtl);

                    _logger.LogInformation("💬 [COM_POSTSYNC] CacheHit=true persisted, Key={Key}", key);
                }
                catch (Exception ex)
                {
                    _logger.LogError("💬 [COM_SYNC_ERROR] CacheHit=true failed, Key={Key}, Error={Error}", key, ex.Message);
                    throw;
                }

                _logger.LogInformation("💬 [COM_AGG] CacheHit=true, Count={Count}, FirstActorName={FirstActorName}", data.Count, data.FirstActorName);
                return true;
            }
        }

        var newData = new CommentReplyAggregationData
        {
            EventType = eventType,
            ObjectId = objectId,
            OwnerId = ownerId,
            FirstActorId = actorId,
            FirstActorName = actorName,
            Count = 1,
            FirstAt = GetVietnamTime(),
            LastAt = GetVietnamTime()
        };

        var newJsonData = JsonSerializer.Serialize(newData);
        _logger.LogInformation("💬 [COM_PRESYNC] FirstEvent, Key={Key}, DataLength={DataLength}, TTL={TTL}, ActorName={ActorName}", 
            key, newJsonData.Length, _cacheTtl, actorName);

        try
        {
            await db.StringSetAsync(key, newJsonData, _cacheTtl);

            _logger.LogInformation("💬 [COM_POSTSYNC] FirstEvent persisted, Key={Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError("💬 [COM_SYNC_ERROR] FirstEvent failed, Key={Key}, Error={Error}", key, ex.Message);
            throw;
        }

        _logger.LogInformation("💬 [COM_AGG] CacheHit=false, FirstEvent, ActorName={ActorName}", actorName);
        return false;
    }

    private static string BuildAggregationKey(string eventType, string objectId, string ownerId)
        => $"comment_reply_agg:{eventType}:{objectId}:{ownerId}";

    private static DateTime GetVietnamTime()
    {
        var utcNow = DateTime.UtcNow;
        var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        return TimeZoneInfo.ConvertTimeFromUtc(utcNow, vietnamTimeZone);
    }
}

public class CommentReplyAggregationData
{
    public string EventType { get; set; } = string.Empty;
    public string ObjectId { get; set; } = string.Empty;
    public string OwnerId { get; set; } = string.Empty;
    public string FirstActorId { get; set; } = string.Empty;
    public string FirstActorName { get; set; } = string.Empty;
    public int Count { get; set; }
    public DateTime FirstAt { get; set; }
    public DateTime LastAt { get; set; }
}

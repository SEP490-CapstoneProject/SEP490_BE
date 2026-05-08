using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Notification.Application.Services;

public class CommentReplyAggregationService
{
    private readonly IDistributedCache _cache;
    private readonly TimeSpan _aggregationWindow;
    private readonly TimeSpan _cacheTtl;
    private readonly ILogger<CommentReplyAggregationService> _logger;

    public CommentReplyAggregationService(IDistributedCache cache, IConfiguration configuration, ILogger<CommentReplyAggregationService> logger)
    {
        _cache = cache;
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
        
        // Remove any old HASH data - IDistributedCache.Remove is more reliable than SetStringAsync overwrite
        var cached = await _cache.GetStringAsync(key, cancellationToken);

        if (cached != null)
        {
            var data = JsonSerializer.Deserialize<CommentReplyAggregationData>(cached);
            if (data != null)
            {
                data.Count++;
                data.LastAt = GetVietnamTime();
                data.FirstAt = GetVietnamTime();  // Sliding window: reset on each event

                await _cache.SetStringAsync(
                    key,
                    JsonSerializer.Serialize(data),
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = _cacheTtl },
                    cancellationToken);

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

        await _cache.SetStringAsync(
            key,
            JsonSerializer.Serialize(newData),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = _cacheTtl },
            cancellationToken);

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

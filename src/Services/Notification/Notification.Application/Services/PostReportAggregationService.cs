using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Notification.Application.Services;

public class PostReportAggregationService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly TimeSpan _aggregationWindow;
    private readonly TimeSpan _cacheTtl;
    private readonly ILogger<PostReportAggregationService> _logger;

    public PostReportAggregationService(IConnectionMultiplexer redis, IConfiguration configuration, ILogger<PostReportAggregationService> logger)
    {
        _redis = redis;
        _logger = logger;
        // Sliding window: prefer seconds (new) over minutes (legacy)
        var windowSeconds = configuration.GetValue<int?>("PostReportAggregation:WindowSeconds");
        if (windowSeconds.HasValue)
        {
            _aggregationWindow = TimeSpan.FromSeconds(windowSeconds.Value);
        }
        else
        {
            var windowMinutes = configuration.GetValue<int?>("PostReportAggregation:WindowMinutes") ?? 10;
            _aggregationWindow = TimeSpan.FromMinutes(windowMinutes);
        }
        _cacheTtl = _aggregationWindow + TimeSpan.FromSeconds(90);
    }

    public async Task<PostReportAggregationResult> TrackReportAsync(int postId, string recipientUserId, CancellationToken cancellationToken = default)
    {
        var key = BuildAggregationKey(postId, recipientUserId);
        var db = _redis.GetDatabase();
        
        // Read from Redis using raw Redis (consistent with flush service)
        var cached = await db.StringGetAsync(key);

        if (cached.HasValue)
        {
            var json = cached.ToString();
            var data = JsonSerializer.Deserialize<PostReportAggregationData>(json);
            if (data != null)
            {
                data.AdditionalCount++;
                data.LastAt = GetVietnamTime();
                data.FirstAt = GetVietnamTime();  // Sliding window: reset on each event

                var jsonData = JsonSerializer.Serialize(data);
                _logger.LogInformation("📋 [RPT_PRESYNC] CacheHit=true, Key={Key}, DataLength={DataLength}, TTL={TTL}", 
                    key, jsonData.Length, _cacheTtl);

                try
                {
                    await db.StringSetAsync(key, jsonData, _cacheTtl);

                    _logger.LogInformation("📋 [RPT_POSTSYNC] CacheHit=true persisted, Key={Key}", key);
                }
                catch (Exception ex)
                {
                    _logger.LogError("📋 [RPT_SYNC_ERROR] CacheHit=true failed, Key={Key}, Error={Error}", key, ex.Message);
                    throw;
                }

                return new PostReportAggregationResult
                {
                    SendImmediateNotification = false
                };
            }
        }

        var newData = new PostReportAggregationData
        {
            PostId = postId,
            RecipientUserId = recipientUserId,
            AdditionalCount = 0,
            FirstAt = GetVietnamTime(),
            LastAt = GetVietnamTime()
        };

        var newJsonData = JsonSerializer.Serialize(newData);
        _logger.LogInformation("📋 [RPT_PRESYNC] FirstEvent, Key={Key}, DataLength={DataLength}, TTL={TTL}", 
            key, newJsonData.Length, _cacheTtl);

        try
        {
            await db.StringSetAsync(key, newJsonData, _cacheTtl);

            _logger.LogInformation("📋 [RPT_POSTSYNC] FirstEvent persisted, Key={Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogError("📋 [RPT_SYNC_ERROR] FirstEvent failed, Key={Key}, Error={Error}", key, ex.Message);
            throw;
        }

        return new PostReportAggregationResult
        {
            SendImmediateNotification = true
        };
    }

    private static string BuildAggregationKey(int postId, string recipientUserId)
        => $"post_report_agg:{postId}:{recipientUserId}";

    private static DateTime GetVietnamTime()
    {
        var utcNow = DateTime.UtcNow;
        var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        return TimeZoneInfo.ConvertTimeFromUtc(utcNow, vietnamTimeZone);
    }
}

public class PostReportAggregationData
{
    public int PostId { get; set; }
    public string RecipientUserId { get; set; } = string.Empty;
    public int AdditionalCount { get; set; }
    public DateTime FirstAt { get; set; }
    public DateTime LastAt { get; set; }
}

public class PostReportAggregationResult
{
    public bool SendImmediateNotification { get; set; }
}

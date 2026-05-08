using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Notification.Application.Services;

public class PostReportAggregationService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<PostReportAggregationService> _logger;
    private readonly TimeSpan _aggregationWindow;
    private readonly TimeSpan _cacheTtl;

    public PostReportAggregationService(IDistributedCache cache, ILogger<PostReportAggregationService> logger, IConfiguration configuration)
    {
        _cache = cache;
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
        var cached = await _cache.GetStringAsync(key, cancellationToken);

        if (cached != null)
        {
            var data = JsonSerializer.Deserialize<PostReportAggregationData>(cached);
            if (data != null)
            {
                data.AdditionalCount++;
                data.LastAt = GetVietnamTime();
                data.FirstAt = GetVietnamTime();  // Sliding window: reset on each event

                try
                {
                    _cache.Remove(key);
                    _logger.LogInformation("📋 [RPT_CACHE_CLEAN] Removed old key {Key}", key);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("📋 [RPT_CACHE_CLEAN_FAIL] Failed to remove key {Key}: {Error}", key, ex.Message);
                }

                await _cache.SetStringAsync(
                    key,
                    JsonSerializer.Serialize(data),
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = _cacheTtl },
                    cancellationToken);

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

        try
        {
            _cache.Remove(key);
            _logger.LogInformation("📋 [RPT_CACHE_CLEAN] Removed old key {Key}", key);
        }
        catch (Exception ex)
        {
            _logger.LogWarning("📋 [RPT_CACHE_CLEAN_FAIL] Failed to remove key {Key}: {Error}", key, ex.Message);
        }

        await _cache.SetStringAsync(
            key,
            JsonSerializer.Serialize(newData),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = _cacheTtl },
            cancellationToken);

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

using System.Text;
using System.Text.Json;
using Notification.Application.Helpers;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Notification.Application.Interfaces;
using Notification.Application.Services;
using Notification.Domain.Entities;
using Notification.Infrastructure.Messaging;
using StackExchange.Redis;

namespace Notification.Infrastructure.Services;

public class AggregationFlushService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<AggregationFlushService> _logger;
    private readonly TimeSpan _pollInterval;
    private readonly TimeSpan _aggregationWindow;
    private readonly TimeSpan _postReportAggregationWindow;

    public AggregationFlushService(
        IServiceScopeFactory scopeFactory,
        IConnectionMultiplexer redis,
        IConfiguration configuration,
        ILogger<AggregationFlushService> logger)
    {
        _scopeFactory = scopeFactory;
        _redis = redis;
        _logger = logger;
        
        var pollSeconds = configuration.GetValue<int?>("FavoriteAggregation:PollIntervalSeconds") ?? 30;
        _pollInterval = TimeSpan.FromSeconds(pollSeconds);
        
        // Sliding window: prefer seconds (new) over minutes (legacy)
        var windowSeconds = configuration.GetValue<int?>("FavoriteAggregation:WindowSeconds");
        if (windowSeconds.HasValue)
        {
            _aggregationWindow = TimeSpan.FromSeconds(windowSeconds.Value);
        }
        else
        {
            var windowMinutes = configuration.GetValue<int?>("FavoriteAggregation:WindowMinutes") ?? 3;
            _aggregationWindow = TimeSpan.FromMinutes(windowMinutes);
        }

        var postReportWindowMinutes = configuration.GetValue<int?>("PostReportAggregation:WindowMinutes") ?? 10;
        _postReportAggregationWindow = TimeSpan.FromMinutes(postReportWindowMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AggregationFlushService started. Poll interval: {PollInterval}", _pollInterval);
        
        // Subscribe to Redis key expiration events for real-time notification triggers (fire and forget)
        try
        {
            var subscriber = _redis.GetSubscriber();
            await subscriber.SubscribeAsync("__keyevent@0__:expired", (channel, value) =>
            {
                // Fire and forget - don't await inside the callback
                var keyName = value.ToString();
                if (keyName.StartsWith("favorite_agg:") || keyName.StartsWith("comment_reply_agg:"))
                {
                    _logger.LogDebug("Redis key expired: {KeyName}, polling will handle aggregation flush", keyName);
                }
            });
            _logger.LogInformation("Redis key expiration listener subscribed to __keyevent@0__:expired");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to setup Redis key expiration listener. Will fall back to polling.");
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredAggregationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing expired aggregations");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }
    }

    private async Task ProcessExpiredAggregationsAsync(CancellationToken cancellationToken)
    {
        var db = _redis.GetDatabase();
        var server = _redis.GetServer(_redis.GetEndPoints().First());

        var favoriteKeys = server.Keys(pattern: "favorite_agg:*").ToList();
        _logger.LogInformation("🔔 [FAV_FLUSH_SCAN] Found {FavoriteKeyCount} favorite aggregations", favoriteKeys.Count);
        
        // First pass: Clean up any old HASH format data before processing STRING keys
        // This prevents WRONGTYPE errors when trying to read old HASH data as STRING
        foreach (var key in favoriteKeys)
        {
            if (cancellationToken.IsCancellationRequested) break;
            try
            {
                // Try to read as STRING first
                var type = await db.KeyTypeAsync(key);
                if (type == StackExchange.Redis.RedisType.Hash)
                {
                    // Old HASH format found - delete it immediately
                    await db.KeyDeleteAsync(key);
                    _logger.LogWarning("🔔 [FAV_PRECLEAN] Deleted old HASH format key {Key}", key.ToString());
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("🔔 [FAV_PRECLEAN_FAIL] Error checking key type {Key}: {Error}", key.ToString(), ex.Message);
            }
        }
        
        foreach (var key in favoriteKeys)
        {
            if (cancellationToken.IsCancellationRequested) break;
            await ProcessFavoriteAggregationKeyAsync(db, key, cancellationToken);
        }

        var commentKeys = server.Keys(pattern: "comment_reply_agg:*").ToList();
        _logger.LogInformation("💬 [COM_FLUSH_SCAN] Found {CommentKeyCount} comment aggregations", commentKeys.Count);
        
        // First pass: Clean up any old HASH format data before processing STRING keys
        // This prevents WRONGTYPE errors when trying to read old HASH data as STRING
        foreach (var key in commentKeys)
        {
            if (cancellationToken.IsCancellationRequested) break;
            try
            {
                // Try to read as STRING first
                var type = await db.KeyTypeAsync(key);
                if (type == StackExchange.Redis.RedisType.Hash)
                {
                    // Old HASH format found - delete it immediately
                    await db.KeyDeleteAsync(key);
                    _logger.LogWarning("💬 [COM_PRECLEAN] Deleted old HASH format key {Key}", key.ToString());
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("💬 [COM_PRECLEAN_FAIL] Error checking key type {Key}: {Error}", key.ToString(), ex.Message);
            }
        }
        
        foreach (var key in commentKeys)
        {
            if (cancellationToken.IsCancellationRequested) break;
            await ProcessCommentReplyAggregationKeyAsync(db, key, cancellationToken);
        }

        var reportKeys = server.Keys(pattern: "post_report_agg:*").ToList();
        
        // First pass: Clean up any old HASH format data before processing STRING keys
        // This prevents WRONGTYPE errors when trying to read old HASH data as STRING
        foreach (var key in reportKeys)
        {
            if (cancellationToken.IsCancellationRequested) break;
            try
            {
                // Try to read as STRING first
                var type = await db.KeyTypeAsync(key);
                if (type == StackExchange.Redis.RedisType.Hash)
                {
                    // Old HASH format found - delete it immediately
                    await db.KeyDeleteAsync(key);
                    _logger.LogWarning("📋 [RPT_PRECLEAN] Deleted old HASH format key {Key}", key.ToString());
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning("📋 [RPT_PRECLEAN_FAIL] Error checking key type {Key}: {Error}", key.ToString(), ex.Message);
            }
        }
        
        foreach (var key in reportKeys)
        {
            if (cancellationToken.IsCancellationRequested) break;
            await ProcessPostReportAggregationKeyAsync(db, key, cancellationToken);
        }
    }

    private async Task ProcessFavoriteAggregationKeyAsync(StackExchange.Redis.IDatabase db, StackExchange.Redis.RedisKey key, CancellationToken cancellationToken)
    {
        try
        {
            var rawData = await db.StringGetAsync(key);
            _logger.LogInformation("🔔 [FAV_FLUSH_READ] Key={Key}, HasValue={HasValue}", key.ToString(), rawData.HasValue);
            
            if (!rawData.HasValue) return;

            var json = rawData.ToString();
            if (string.IsNullOrWhiteSpace(json)) 
            {
                _logger.LogWarning("🔔 [FAV_FLUSH_READ] Empty JSON for key {Key}", key);
                return;
            }

            var data = JsonSerializer.Deserialize<AggregationData>(json);
            if (data == null) 
            {
                _logger.LogWarning("🔔 [FAV_FLUSH_READ] Deserialization failed for key {Key}", key);
                return;
            }

            var timeDiff = GetVietnamTime() - data.FirstAt;
            _logger.LogInformation("🔔 [FAV_FLUSH_WINDOW] PostId={PostId}, Count={Count}, TimeSinceFirst={TimeDiff}ms, WindowMs={Window}ms",
                data.PostId, data.Count, timeDiff.TotalMilliseconds, _aggregationWindow.TotalMilliseconds);

            if (timeDiff < _aggregationWindow) 
            {
                _logger.LogInformation("🔔 [FAV_FLUSH_WINDOW_NOT_EXPIRED] Skipping, time {TimeDiff}ms < window {Window}ms", 
                    timeDiff.TotalMilliseconds, _aggregationWindow.TotalMilliseconds);
                return;
            }

            await CreateFavoriteAggregatedNotificationAsync(data, cancellationToken);
            await db.KeyDeleteAsync(key);

            _logger.LogInformation("🔔 [FAV_FLUSH_SUCCESS] Flushed favorite aggregation for post {PostId}, owner {OwnerId}, count {Count}",
                data.PostId, data.OwnerId, data.Count);
        }
        catch (StackExchange.Redis.RedisServerException ex) when (ex.Message.Contains("WRONGTYPE"))
        {
            // Old data stored as hash, delete and skip
            _logger.LogWarning("🔔 [FAV_FLUSH_WRONGTYPE] Deleting corrupted hash key {Key}", key.ToString());
            try
            {
                await db.KeyDeleteAsync(key);
            }
            catch { /* ignore cleanup errors */ }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "🔔 [FAV_FLUSH_ERROR] Error processing favorite aggregation key {Key}", key.ToString());
        }
    }

     private async Task ProcessCommentReplyAggregationKeyAsync(StackExchange.Redis.IDatabase db, StackExchange.Redis.RedisKey key, CancellationToken cancellationToken)
     {
         try
         {
             var rawData = await db.StringGetAsync(key);
             _logger.LogInformation("💬 [COM_FLUSH_READ] Key={Key}, HasValue={HasValue}", key.ToString(), rawData.HasValue);
             
             if (!rawData.HasValue) return;

             var json = rawData.ToString();
             if (string.IsNullOrWhiteSpace(json)) 
             {
                 _logger.LogWarning("💬 [COM_FLUSH_READ] Empty JSON for key {Key}", key);
                 return;
             }

             var data = JsonSerializer.Deserialize<CommentReplyAggregationData>(json);
             if (data == null) 
             {
                 _logger.LogWarning("💬 [COM_FLUSH_READ] Deserialization failed for key {Key}", key);
                 return;
             }

             var timeDiff = GetVietnamTime() - data.FirstAt;
             _logger.LogInformation("💬 [COM_FLUSH_WINDOW] Count={Count}, FirstActorName={FirstActorName}, TimeSinceFirst={TimeDiff}ms",
                 data.Count, data.FirstActorName, timeDiff.TotalMilliseconds);

             if (timeDiff < _aggregationWindow) 
             {
                 _logger.LogInformation("💬 [COM_FLUSH_WINDOW_NOT_EXPIRED] Skipping, time {TimeDiff}ms < window {Window}ms", 
                     timeDiff.TotalMilliseconds, _aggregationWindow.TotalMilliseconds);
                 return;
             }

             await CreateCommentReplyAggregatedNotificationAsync(data, cancellationToken);
             await db.KeyDeleteAsync(key);

             _logger.LogInformation("💬 [COM_FLUSH_SUCCESS] Flushed comment/reply aggregation for object {ObjectId}, owner {OwnerId}, count {Count}, type {EventType}, actorName={ActorName}",
                 data.ObjectId, data.OwnerId, data.Count, data.EventType, data.FirstActorName);
         }
         catch (StackExchange.Redis.RedisServerException ex) when (ex.Message.Contains("WRONGTYPE"))
         {
             // Old data stored as hash, delete and skip
             _logger.LogWarning("💬 [COM_FLUSH_WRONGTYPE] Deleting corrupted hash key {Key}", key.ToString());
             try
             {
                 await db.KeyDeleteAsync(key);
             }
             catch { /* ignore cleanup errors */ }
         }
         catch (Exception ex)
         {
             _logger.LogError(ex, "💬 [COM_FLUSH_ERROR] Error processing comment/reply aggregation key {Key}", key.ToString());
         }
     }

    private async Task ProcessPostReportAggregationKeyAsync(StackExchange.Redis.IDatabase db, StackExchange.Redis.RedisKey key, CancellationToken cancellationToken)
    {
        try
        {
            var rawData = await db.StringGetAsync(key);
            if (!rawData.HasValue) return;

            var json = rawData.ToString();
            if (string.IsNullOrWhiteSpace(json)) return;

            var data = JsonSerializer.Deserialize<PostReportAggregationData>(json);
            if (data == null) return;

            if (GetVietnamTime() - data.FirstAt < _postReportAggregationWindow) return;

            if (data.AdditionalCount <= 0)
            {
                await db.KeyDeleteAsync(key);
                return;
            }

            await CreatePostReportAggregatedNotificationAsync(data, cancellationToken);
            await db.KeyDeleteAsync(key);

            _logger.LogInformation("Flushed post report aggregation for post {PostId}, recipient {RecipientUserId}, count {Count}",
                data.PostId, data.RecipientUserId, data.AdditionalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing post report aggregation key {Key}", key.ToString());
        }
    }

    private async Task CreateFavoriteAggregatedNotificationAsync(AggregationData data, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var content = data.Count == 1 
            ? $"{data.FirstActorName} đã thích bài viết của bạn"
            : $"{data.Count} người đã thích bài viết của bạn";

        var entity = new NotificationEntity
        {
            UserId = data.OwnerId,
            Title = "Lượt thích mới",
            Content = content,
            Type = "POST_FAVORITE",
            ObjectId = data.PostId.ToString(),
            ActorId = data.Count == 1 ? data.FirstActorId : null,
            ActorName = data.Count == 1 ? data.FirstActorName : null,
            ActorAvatar = null,
            ActorType = data.Count == 1 ? "USER" : "SYSTEM",
            CreatedAt = GetVietnamTime(),
            IsRead = false
        };

        _logger.LogInformation("🔔 [FAV_CREATE] Creating notification: Count={Count}, ActorId={ActorId}, ActorName={ActorName}, ActorType={ActorType}",
            data.Count, entity.ActorId, entity.ActorName, entity.ActorType);

        await PublishNotificationAsync(scope.ServiceProvider, notificationService, entity);
    }

    private async Task CreateCommentReplyAggregatedNotificationAsync(CommentReplyAggregationData data, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var isReplyEvent = string.Equals(data.EventType, "post.reply.created", StringComparison.OrdinalIgnoreCase);
        var title = isReplyEvent ? "Trả lời mới" : "Bình luận mới";
        var content = isReplyEvent
            ? (data.Count == 1
                ? NotificationContentTemplates.PostReply.NewReply(data.FirstActorName)
                : NotificationContentTemplates.PostReply.MultipleReplies(data.Count))
            : (data.Count == 1
                ? NotificationContentTemplates.PostComment.NewComment(data.FirstActorName)
                : NotificationContentTemplates.PostComment.MultipleComments(data.Count));

        var entity = new NotificationEntity
        {
            UserId = data.OwnerId,
            Title = title,
            Content = content,
            Type = "COMMUNITY",
            ObjectId = data.ObjectId,
            ActorId = data.Count == 1 ? data.FirstActorId : null,
            ActorName = data.Count == 1 ? data.FirstActorName : null,
            ActorAvatar = null,
            ActorType = data.Count == 1 ? "USER" : "SYSTEM",
            CreatedAt = GetVietnamTime(),
            IsRead = false
        };

        _logger.LogInformation("💬 [COM_CREATE] Creating notification: EventType={EventType}, Count={Count}, ActorId={ActorId}, ActorName={ActorName}, ActorType={ActorType}",
            data.EventType, data.Count, entity.ActorId, entity.ActorName, entity.ActorType);

        await PublishNotificationAsync(scope.ServiceProvider, notificationService, entity);
    }

    private async Task CreatePostReportAggregatedNotificationAsync(PostReportAggregationData data, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var entity = new NotificationEntity
        {
            UserId = data.RecipientUserId,
            Title = "Bài đăng bị báo cáo",
            Content = data.AdditionalCount == 1
                ? $"Bài đăng #{data.PostId} có thêm 1 báo cáo mới cần được kiểm duyệt."
                : $"Bài đăng #{data.PostId} có thêm {data.AdditionalCount} báo cáo mới cần được kiểm duyệt.",
            Type = "COMMUNITY_REPORT_REVIEW",
            ObjectId = data.PostId.ToString(),
            ActorId = null,
            ActorType = "SYSTEM",
            CreatedAt = GetVietnamTime(),
            IsRead = false
        };

        await PublishNotificationAsync(scope.ServiceProvider, notificationService, entity);
    }

    private static async Task PublishNotificationAsync(
        IServiceProvider services,
        INotificationService notificationService,
        NotificationEntity entity)
    {
        await notificationService.CreateNotificationAsync(entity);

        var createdEvent = await notificationService.BuildCreatedEventAsync(entity);
        var eventPublisher = services.GetRequiredService<INotificationEventPublisher>();
        await eventPublisher.PublishNotificationCreatedAsync(createdEvent);

        var cache = services.GetService<IDistributedCache>();
        if (cache != null)
        {
            await cache.RemoveAsync($"unread:{entity.UserId}");
        }
    }

    private static DateTime GetVietnamTime()
    {
        var utcNow = DateTime.UtcNow;
        var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        return TimeZoneInfo.ConvertTimeFromUtc(utcNow, vietnamTimeZone);
    }
}

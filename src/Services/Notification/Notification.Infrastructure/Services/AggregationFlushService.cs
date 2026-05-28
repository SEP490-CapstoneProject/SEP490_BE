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
    private readonly IActorResolverClient _actorResolverClient;
    private readonly ILogger<AggregationFlushService> _logger;
    private readonly TimeSpan _pollInterval;
    private readonly TimeSpan _aggregationWindow;
    private readonly TimeSpan _postReportAggregationWindow;

    public AggregationFlushService(
        IServiceScopeFactory scopeFactory,
        IConnectionMultiplexer redis,
        IActorResolverClient actorResolverClient,
        IConfiguration configuration,
        ILogger<AggregationFlushService> logger)
    {
        _scopeFactory = scopeFactory;
        _redis = redis;
        _actorResolverClient = actorResolverClient;
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
        
        foreach (var key in favoriteKeys)
        {
            if (cancellationToken.IsCancellationRequested) break;
            await ProcessFavoriteAggregationKeyAsync(db, key, cancellationToken);
        }

        var commentKeys = server.Keys(pattern: "comment_reply_agg:*").ToList();
        
        foreach (var key in commentKeys)
        {
            if (cancellationToken.IsCancellationRequested) break;
            await ProcessCommentReplyAggregationKeyAsync(db, key, cancellationToken);
        }

        var reportKeys = server.Keys(pattern: "post_report_agg:*").ToList();
        
        foreach (var key in reportKeys)
        {
            if (cancellationToken.IsCancellationRequested) break;
            await ProcessPostReportAggregationKeyAsync(db, key, cancellationToken);
        }
    }

    private async Task<bool> TryCleanupOldHashFormatAsync(StackExchange.Redis.IDatabase db, StackExchange.Redis.RedisKey key, string prefix)
    {
        try
        {
            var type = await db.KeyTypeAsync(key);
            if (type == StackExchange.Redis.RedisType.Hash)
            {
                await db.KeyDeleteAsync(key);
                _logger.LogWarning("{Prefix} Cleaned old HASH format key: {Key}", prefix, key.ToString());
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "{Prefix} Error during HASH cleanup for key {Key}", prefix, key.ToString());
        }
        return false;
    }

    private async Task ProcessFavoriteAggregationKeyAsync(StackExchange.Redis.IDatabase db, StackExchange.Redis.RedisKey key, CancellationToken cancellationToken)
    {
        try
        {
            if (await TryCleanupOldHashFormatAsync(db, key, "🔔 [FAV]"))
                return;

            var rawData = await db.StringGetAsync(key);
            
            if (!rawData.HasValue) 
                return;

            var json = rawData.ToString();
            if (string.IsNullOrWhiteSpace(json)) 
                return;

            AggregationData? data;
            try
            {
                data = JsonSerializer.Deserialize<AggregationData>(json);
                if (data == null) 
                    return;
            }
            catch (JsonException jex)
            {
                _logger.LogError(jex, "🔔 [FAV_DESERIALIZE_ERROR] JSON parse failed for key {Key}", key);
                return;
            }

            var timeDiff = GetVietnamTime() - data.FirstAt;
            if (timeDiff < _aggregationWindow) 
                return;

            await CreateFavoriteAggregatedNotificationAsync(data, cancellationToken);
            await db.KeyDeleteAsync(key);

            _logger.LogInformation("🔔 [FAV_FLUSH_SUCCESS] Flushed favorite aggregation for post {PostId}, owner {OwnerId}, count {Count}",
                data.PostId, data.OwnerId, data.Count);
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
            if (await TryCleanupOldHashFormatAsync(db, key, "💬 [COM]"))
                return;

            var rawData = await db.StringGetAsync(key);
            
            if (!rawData.HasValue) 
                return;

            var json = rawData.ToString();
            if (string.IsNullOrWhiteSpace(json)) 
                return;

            CommentReplyAggregationData? data;
            try
            {
                data = JsonSerializer.Deserialize<CommentReplyAggregationData>(json);
                if (data == null) 
                    return;
            }
            catch (JsonException jex)
            {
                _logger.LogError(jex, "💬 [COM_DESERIALIZE_ERROR] JSON parse failed for key {Key}", key);
                return;
            }

            var timeDiff = GetVietnamTime() - data.FirstAt;
            if (timeDiff < _aggregationWindow) 
                return;

            await CreateCommentReplyAggregatedNotificationAsync(data, cancellationToken);
            await db.KeyDeleteAsync(key);

            _logger.LogInformation("💬 [COM_FLUSH_SUCCESS] Flushed comment/reply aggregation for object {ObjectId}, owner {OwnerId}, count {Count}, type {EventType}",
                data.ObjectId, data.OwnerId, data.Count, data.EventType);
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
            if (await TryCleanupOldHashFormatAsync(db, key, "📋 [RPT]"))
                return;

            var rawData = await db.StringGetAsync(key);
            
            if (!rawData.HasValue) 
                return;

            var json = rawData.ToString();
            if (string.IsNullOrWhiteSpace(json)) 
                return;

            PostReportAggregationData? data;
            try
            {
                data = JsonSerializer.Deserialize<PostReportAggregationData>(json);
                if (data == null) 
                    return;
            }
            catch (JsonException jex)
            {
                _logger.LogError(jex, "📋 [RPT_DESERIALIZE_ERROR] JSON parse failed for key {Key}", key);
                return;
            }

            if (GetVietnamTime() - data.FirstAt < _postReportAggregationWindow) 
                return;

            if (data.AdditionalCount <= 0)
            {
                await db.KeyDeleteAsync(key);
                return;
            }

            await CreatePostReportAggregatedNotificationAsync(data, cancellationToken);
            await db.KeyDeleteAsync(key);

            _logger.LogInformation("📋 [RPT_FLUSH_SUCCESS] Flushed post report aggregation for post {PostId}, recipient {RecipientUserId}, count {Count}",
                data.PostId, data.RecipientUserId, data.AdditionalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "📋 [RPT_FLUSH_ERROR] Error processing post report aggregation key {Key}", key.ToString());
        }
    }

    private async Task CreateFavoriteAggregatedNotificationAsync(AggregationData data, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        _logger.LogInformation("🔔 [FAV_NOTIFICATION_CREATE] Creating favorite notification. PostId={PostId}, Count={Count}, OwnerId={OwnerId}",
            data.PostId, data.Count, data.OwnerId);

        string? actorName = data.FirstActorName;
        string? actorAvatar = null;
        
        if (data.Count == 1 && !string.IsNullOrEmpty(data.FirstActorId))
        {
            try
            {
                var actor = await _actorResolverClient.ResolveActorAsync(data.FirstActorId, "USER");
                if (actor != null)
                {
                    actorName = actor.Name ?? data.FirstActorName;
                    actorAvatar = actor.Avatar;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "🔔 [FAV_ACTOR_RESOLVE_FAILED] Failed to resolve actor {ActorId}", data.FirstActorId);
            }
        }

        var content = data.Count == 1 
            ? $"{actorName} đã thích bài viết của bạn"
            : $"{data.Count} người đã thích bài viết của bạn";

        var entity = new NotificationEntity
        {
            UserId = data.OwnerId,
            Title = "Lượt thích mới",
            Content = content,
            Type = "POST_FAVORITE",
            ObjectId = data.PostId.ToString(),
            ActorId = data.Count == 1 ? data.FirstActorId : null,
            ActorName = data.Count == 1 ? actorName : null,
            ActorAvatar = data.Count == 1 ? actorAvatar : null,
            ActorType = "USER",
            CreatedAt = GetVietnamTime(),
            IsRead = false
        };

        var publishingService = scope.ServiceProvider.GetRequiredService<INotificationPublishingService>();
        await PublishNotificationAsync(scope.ServiceProvider, notificationService, entity, publishingService);
    }

    private async Task CreateCommentReplyAggregatedNotificationAsync(CommentReplyAggregationData data, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

        var isReplyEvent = string.Equals(data.EventType, "post.reply.created", StringComparison.OrdinalIgnoreCase);
        
        _logger.LogInformation("💬 [COM_NOTIFICATION_CREATE] Creating comment/reply notification. EventType={EventType}, Count={Count}, OwnerId={OwnerId}",
            data.EventType, data.Count, data.OwnerId);

        string? actorName = data.FirstActorName;
        string? actorAvatar = null;
        
        if (data.Count == 1 && !string.IsNullOrEmpty(data.FirstActorId))
        {
            try
            {
                var actor = await _actorResolverClient.ResolveActorAsync(data.FirstActorId, "USER");
                if (actor != null)
                {
                    actorName = actor.Name ?? data.FirstActorName;
                    actorAvatar = actor.Avatar;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "💬 [COM_ACTOR_RESOLVE_FAILED] Failed to resolve actor {ActorId}", data.FirstActorId);
            }
        }

        var title = isReplyEvent ? "Trả lời mới" : "Bình luận mới";
        var content = isReplyEvent
            ? (data.Count == 1
                ? NotificationContentTemplates.PostReply.NewReply(actorName)
                : NotificationContentTemplates.PostReply.MultipleReplies(data.Count))
            : (data.Count == 1
                ? NotificationContentTemplates.PostComment.NewComment(actorName)
                : NotificationContentTemplates.PostComment.MultipleComments(data.Count));

        var entity = new NotificationEntity
        {
            UserId = data.OwnerId,
            Title = title,
            Content = content,
            Type = "COMMUNITY",
            ObjectId = data.ObjectId,
            ActorId = data.Count == 1 ? data.FirstActorId : null,
            ActorName = data.Count == 1 ? actorName : null,
            ActorAvatar = data.Count == 1 ? actorAvatar : null,
            ActorType = "USER",
            CreatedAt = GetVietnamTime(),
            IsRead = false
        };

        var publishingService = scope.ServiceProvider.GetRequiredService<INotificationPublishingService>();
        await PublishNotificationAsync(scope.ServiceProvider, notificationService, entity, publishingService);
        
        _logger.LogInformation("💬 [COM_PUBLISHED] Comment/reply notification published. NotificationId={NotificationId}, UserId={UserId}, EventType={EventType}, Type={Type}",
            entity.Id, entity.UserId, data.EventType, entity.Type);
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

        var publishingService = scope.ServiceProvider.GetRequiredService<INotificationPublishingService>();
        await PublishNotificationAsync(scope.ServiceProvider, notificationService, entity, publishingService);
    }

    private static async Task PublishNotificationAsync(
        IServiceProvider services,
        INotificationService notificationService,
        NotificationEntity entity,
        INotificationPublishingService publishingService)
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

        // Trigger FCM (fire-and-forget pattern - non-blocking)
        _ = publishingService.SendFcmOnlyAsync(entity).ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                // Exceptions are already logged in SendFcmOnlyAsync
            }
        });
    }

    private static DateTime GetVietnamTime()
    {
        var utcNow = DateTime.UtcNow;
        var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        return TimeZoneInfo.ConvertTimeFromUtc(utcNow, vietnamTimeZone);
    }
}

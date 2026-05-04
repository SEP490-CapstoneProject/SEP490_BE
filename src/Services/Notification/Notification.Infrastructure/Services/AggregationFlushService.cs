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

        foreach (var key in server.Keys(pattern: "favorite_agg:*"))
        {
            if (cancellationToken.IsCancellationRequested) break;
            await ProcessFavoriteAggregationKeyAsync(db, key, cancellationToken);
        }

        foreach (var key in server.Keys(pattern: "comment_reply_agg:*"))
        {
            if (cancellationToken.IsCancellationRequested) break;
            await ProcessCommentReplyAggregationKeyAsync(db, key, cancellationToken);
        }

        foreach (var key in server.Keys(pattern: "post_report_agg:*"))
        {
            if (cancellationToken.IsCancellationRequested) break;
            await ProcessPostReportAggregationKeyAsync(db, key, cancellationToken);
        }
    }

    private async Task ProcessFavoriteAggregationKeyAsync(StackExchange.Redis.IDatabase db, StackExchange.Redis.RedisKey key, CancellationToken cancellationToken)
    {
        try
        {
            var rawData = await db.HashGetAsync(key, "data");
            if (!rawData.HasValue) return;

            var bytes = (byte[]?)rawData;
            if (bytes == null || bytes.Length == 0) return;

            var json = Encoding.UTF8.GetString(bytes);
            var data = JsonSerializer.Deserialize<AggregationData>(json);
            if (data == null) return;

            if (GetVietnamTime() - data.FirstAt < _aggregationWindow) return;

            await CreateFavoriteAggregatedNotificationAsync(data, cancellationToken);
            await db.KeyDeleteAsync(key);

            _logger.LogInformation("Flushed favorite aggregation for post {PostId}, owner {OwnerId}, count {Count}",
                data.PostId, data.OwnerId, data.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing favorite aggregation key {Key}", key.ToString());
        }
    }

    private async Task ProcessCommentReplyAggregationKeyAsync(StackExchange.Redis.IDatabase db, StackExchange.Redis.RedisKey key, CancellationToken cancellationToken)
    {
        try
        {
            var rawData = await db.HashGetAsync(key, "data");
            if (!rawData.HasValue) return;

            var bytes = (byte[]?)rawData;
            if (bytes == null || bytes.Length == 0) return;

            var json = Encoding.UTF8.GetString(bytes);
            var data = JsonSerializer.Deserialize<CommentReplyAggregationData>(json);
            if (data == null) return;

            if (GetVietnamTime() - data.FirstAt < _aggregationWindow) return;

            await CreateCommentReplyAggregatedNotificationAsync(data, cancellationToken);
            await db.KeyDeleteAsync(key);

            _logger.LogInformation("Flushed comment/reply aggregation for object {ObjectId}, owner {OwnerId}, count {Count}, type {EventType}",
                data.ObjectId, data.OwnerId, data.Count, data.EventType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing comment/reply aggregation key {Key}", key.ToString());
        }
    }

    private async Task ProcessPostReportAggregationKeyAsync(StackExchange.Redis.IDatabase db, StackExchange.Redis.RedisKey key, CancellationToken cancellationToken)
    {
        try
        {
            var rawData = await db.HashGetAsync(key, "data");
            if (!rawData.HasValue) return;

            var bytes = (byte[]?)rawData;
            if (bytes == null || bytes.Length == 0) return;

            var json = Encoding.UTF8.GetString(bytes);
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
            ActorType = data.Count == 1 ? "USER" : "SYSTEM",
            CreatedAt = GetVietnamTime(),
            IsRead = false
        };

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
            ActorType = data.Count == 1 ? "USER" : "SYSTEM",
            CreatedAt = GetVietnamTime(),
            IsRead = false
        };

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

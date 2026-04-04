using System.Text.Json;
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
        
        var windowMinutes = configuration.GetValue<int?>("FavoriteAggregation:WindowMinutes") ?? 3;
        _aggregationWindow = TimeSpan.FromMinutes(windowMinutes);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AggregationFlushService started. Poll interval: {PollInterval}", _pollInterval);

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

        // Scan for favorite aggregation keys using Scan instead of ScanAsync
        var keys = server.Keys(pattern: "favorite_agg:*");
        
        foreach (var key in keys)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                var value = await db.StringGetAsync(key);
                if (!value.HasValue) continue;

                var data = JsonSerializer.Deserialize<AggregationData>(value!);
                if (data == null) continue;

                // Check if aggregation window has expired
                var age = GetVietnamTime() - data.FirstAt;
                if (age >= _aggregationWindow)
                {
                    await CreateAggregatedNotificationAsync(data, cancellationToken);
                    await db.KeyDeleteAsync(key);
                    
                    _logger.LogInformation("Flushed aggregation for post {PostId}, owner {OwnerId}, count {Count}", 
                        data.PostId, data.OwnerId, data.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing aggregation key {Key}", key.ToString());
            }
        }
    }

    private async Task CreateAggregatedNotificationAsync(AggregationData data, CancellationToken cancellationToken)
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

        await notificationService.CreateNotificationAsync(entity);

        // Create notification event for realtime
        var createdEvent = await notificationService.BuildCreatedEventAsync(entity);
        var eventPublisher = scope.ServiceProvider.GetRequiredService<INotificationEventPublisher>();
        await eventPublisher.PublishNotificationCreatedAsync(createdEvent);
    }

    private static DateTime GetVietnamTime()
    {
        var utcNow = DateTime.UtcNow;
        var vietnamTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
        return TimeZoneInfo.ConvertTimeFromUtc(utcNow, vietnamTimeZone);
    }
}
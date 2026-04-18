using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Notification.Application.Helpers;
using Notification.Application.Interfaces;
using Notification.Application.Services;
using Notification.Domain.Entities;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RecruitmentPlatform.Contracts.Time;

namespace Notification.Infrastructure.Messaging;

public class RabbitMQConsumer : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<RabbitMQConsumer> _logger;

    private IConnection? _connection;
    private IChannel? _channel;

    private const string Exchange = "skillsnap.events";
    private const string Queue = "notification.events";
    private const string DlxExchange = "skillsnap.events.dlx";
    private const string DlqQueue = "notification.events.dlq";

    private static readonly string[] BindingKeys = { "post.#", "connection.*", "portfolio.*", "job.*", "system.*" };
    private static readonly HashSet<string> NotificationEventTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "post.favorite",
        "post.comment.created",
        "post.reply.created",
        "post.report.removed",
        "post.report.created"
    };

    public RabbitMQConsumer(
        IServiceScopeFactory scopeFactory,
        IConfiguration config,
        ILogger<RabbitMQConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _config = config;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var host = _config["RabbitMQ:HostName"] ?? _config["RabbitMQ:Host"] ?? "localhost";
        var userName = _config["RabbitMQ:UserName"] ?? _config["RabbitMQ:Username"] ?? "guest";
        var factory = new ConnectionFactory
        {
            HostName = host,
            UserName = userName,
            Password = _config["RabbitMQ:Password"] ?? "guest",
            VirtualHost = _config["RabbitMQ:VirtualHost"] ?? "/",
            Port = int.TryParse(_config["RabbitMQ:Port"], out var port) ? port : 5672
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _connection = await factory.CreateConnectionAsync(stoppingToken);
                _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

                await _channel.ExchangeDeclareAsync(DlxExchange, ExchangeType.Direct, durable: true, cancellationToken: stoppingToken);
                await _channel.QueueDeclareAsync(DlqQueue, durable: true, exclusive: false, autoDelete: false, cancellationToken: stoppingToken);
                await _channel.QueueBindAsync(DlqQueue, DlxExchange, DlqQueue, cancellationToken: stoppingToken);

                await _channel.ExchangeDeclareAsync(Exchange, ExchangeType.Topic, durable: true, cancellationToken: stoppingToken);

                var queueArgs = new Dictionary<string, object?>
                {
                    { "x-dead-letter-exchange", DlxExchange },
                    { "x-dead-letter-routing-key", DlqQueue }
                };
                await _channel.QueueDeclareAsync(Queue, durable: true, exclusive: false, autoDelete: false, arguments: queueArgs, cancellationToken: stoppingToken);

                foreach (var key in BindingKeys)
                    await _channel.QueueBindAsync(Queue, Exchange, key, cancellationToken: stoppingToken);

                await _channel.BasicQosAsync(0, 10, false, stoppingToken);

                var consumer = new AsyncEventingBasicConsumer(_channel);
                consumer.ReceivedAsync += OnMessageReceivedAsync;

                await _channel.BasicConsumeAsync(Queue, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);

                _logger.LogInformation("RabbitMQ consumer started on queue: {Queue}", Queue);
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RabbitMQ consumer fatal error. Retrying in 10 seconds");
                try
                {
                    if (_channel is not null)
                    {
                        await _channel.CloseAsync(stoppingToken);
                        _channel = null;
                    }
                    if (_connection is not null)
                    {
                        await _connection.CloseAsync(stoppingToken);
                        _connection = null;
                    }
                }
                catch (Exception closeEx)
                {
                    _logger.LogWarning(closeEx, "Error closing RabbitMQ resources");
                }

                await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
            }
        }
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        if (_channel is null) return;

        try
        {
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());
            var evt = JsonSerializer.Deserialize<NotificationEvent>(body, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (evt is null)
            {
                await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
                return;
            }

            using var scope = _scopeFactory.CreateScope();

            if (string.Equals(evt.EventType, "post.report.created", StringComparison.OrdinalIgnoreCase))
            {
                await HandlePostReportCreatedAsync(scope.ServiceProvider, evt);
                await _channel.BasicAckAsync(ea.DeliveryTag, false);
                return;
            }

            if (string.IsNullOrEmpty(evt.UserId))
            {
                await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
                return;
            }
            
            if (!string.IsNullOrWhiteSpace(evt.ActorId) &&
                string.Equals(evt.UserId, evt.ActorId, StringComparison.OrdinalIgnoreCase))
            {
                await _channel.BasicAckAsync(ea.DeliveryTag, false);
                return;
            }

            // Realtime-only counter event: never create notification (prevents unlike => like notification bug)
            if (string.Equals(evt.EventType, "post.favorite.changed", StringComparison.OrdinalIgnoreCase))
            {
                await _channel.BasicAckAsync(ea.DeliveryTag, false);
                return;
            }

            if (!NotificationEventTypes.Contains(evt.EventType))
            {
                _logger.LogDebug("Skip non-notification event type {EventType}", evt.EventType);
                await _channel.BasicAckAsync(ea.DeliveryTag, false);
                return;
            }

            if (string.IsNullOrWhiteSpace(evt.Type) ||
                string.IsNullOrWhiteSpace(evt.Title))
            {
                _logger.LogWarning(
                    "Skip invalid notification event payload. EventType={EventType}, UserId={UserId}, Type={Type}",
                    evt.EventType, evt.UserId, evt.Type);
                await _channel.BasicAckAsync(ea.DeliveryTag, false);
                return;
            }

            // Check if this is a post.favorite event for aggregation
            if (evt.EventType == "post.favorite")
            {
                var aggregationService = scope.ServiceProvider.GetService<FavoriteAggregationService>();
                if (aggregationService != null)
                {
                    var actorName = ExtractActorNameFromContent(evt.Content);
                    var isAggregated = await aggregationService.TryAggregateAsync(
                        int.Parse(evt.ObjectId ?? "0"), 
                        evt.UserId, 
                        evt.ActorId ?? "", 
                        actorName);

                    if (isAggregated)
                    {
                        // Event was aggregated, don't create notification yet
                        await _channel.BasicAckAsync(ea.DeliveryTag, false);
                        return;
                    }
                    // If not aggregated, fall through to create notification immediately
                }
            }
            else if (evt.EventType == "post.comment.created" || evt.EventType == "post.reply.created")
            {
                var aggregationService = scope.ServiceProvider.GetService<CommentReplyAggregationService>();
                if (aggregationService != null && !string.IsNullOrWhiteSpace(evt.ActorId))
                {
                    var actorName = !string.IsNullOrWhiteSpace(evt.Author?.Name) ? evt.Author.Name : "Ai đó";
                    var objectId = string.IsNullOrWhiteSpace(evt.ObjectId) ? "0" : evt.ObjectId;
                    var isAggregated = await aggregationService.TryAggregateAsync(
                        evt.EventType,
                        objectId,
                        evt.UserId,
                        evt.ActorId,
                        actorName);

                    if (isAggregated)
                    {
                        await _channel.BasicAckAsync(ea.DeliveryTag, false);
                        return;
                    }
                }
            }

            var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
            var eventPublisher = scope.ServiceProvider.GetRequiredService<INotificationEventPublisher>();
            var actorNameForContent = !string.IsNullOrWhiteSpace(evt.Author?.Name) ? evt.Author.Name : "Ai đó";

            var entity = new NotificationEntity
            {
                UserId = evt.UserId,
                Title = evt.Title,
                Content = ResolveNotificationContent(evt, actorNameForContent),
                Type = evt.Type,
                ObjectId = evt.ObjectId,
                ActorId = evt.ActorId,
                ActorType = evt.ActorType,
                CreatedAt = evt.CreatedAt == default ? VietnamTime.Now() : evt.CreatedAt
            };

            await notificationService.CreateNotificationAsync(entity);
            var createdEvent = await notificationService.BuildCreatedEventAsync(entity);
            if (!string.IsNullOrWhiteSpace(evt.EventId))
            {
                createdEvent.EventId = evt.EventId;
            }

            var cache = scope.ServiceProvider.GetService<IDistributedCache>();
            if (cache != null)
                await cache.RemoveAsync($"unread:{evt.UserId}");

            await eventPublisher.PublishNotificationCreatedAsync(createdEvent);

            await _channel.BasicAckAsync(ea.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing notification event, sending to DLQ");
            await _channel.BasicNackAsync(ea.DeliveryTag, false, false);
        }
    }

    private static string ExtractActorNameFromContent(string content)
    {
        // Extract actor name from content like "John Doe đã thích bài viết của bạn"
        var index = content.IndexOf(" đã thích");
        if (index > 0)
        {
            return content.Substring(0, index).Trim();
        }
        return "Ai đó";
    }

    private static string ResolveNotificationContent(NotificationEvent evt, string actorName)
    {
        if (string.Equals(evt.EventType, "post.comment.created", StringComparison.OrdinalIgnoreCase))
        {
            return NotificationContentTemplates.PostComment.NewComment(actorName);
        }

        if (string.Equals(evt.EventType, "post.reply.created", StringComparison.OrdinalIgnoreCase))
        {
            return NotificationContentTemplates.PostReply.NewReply(actorName);
        }

        return evt.Content;
    }

    private async Task HandlePostReportCreatedAsync(IServiceProvider services, NotificationEvent evt)
    {
        if (string.IsNullOrWhiteSpace(evt.ObjectId) || !int.TryParse(evt.ObjectId, out var postId))
        {
            _logger.LogWarning("Skip post.report.created because ObjectId is invalid. ObjectId={ObjectId}", evt.ObjectId);
            return;
        }

        var targetRoles = evt.TargetRoles is { Length: > 0 }
            ? evt.TargetRoles
            : ["ADMIN", "MODERATOR"];

        var recipientResolver = services.GetRequiredService<IRecipientResolverClient>();
        var recipients = await recipientResolver.GetActiveUserIdsByRolesAsync(targetRoles);
        if (recipients.Count == 0)
        {
            _logger.LogInformation("No recipients found for post.report.created. PostId={PostId}, Roles={Roles}", postId, string.Join(",", targetRoles));
            return;
        }

        var aggregation = services.GetRequiredService<PostReportAggregationService>();
        var notificationService = services.GetRequiredService<INotificationService>();
        var eventPublisher = services.GetRequiredService<INotificationEventPublisher>();
        var cache = services.GetService<IDistributedCache>();

        foreach (var recipientUserId in recipients)
        {
            if (!string.IsNullOrWhiteSpace(evt.ActorId) &&
                string.Equals(recipientUserId, evt.ActorId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var tracking = await aggregation.TrackReportAsync(postId, recipientUserId);
            if (!tracking.SendImmediateNotification)
            {
                continue;
            }

            var entity = new NotificationEntity
            {
                UserId = recipientUserId,
                Title = "Bài đăng bị báo cáo",
                Content = $"Bài đăng #{postId} có báo cáo mới cần được kiểm duyệt.",
                Type = "COMMUNITY_REPORT_REVIEW",
                ObjectId = postId.ToString(),
                ActorId = evt.ActorId,
                ActorType = string.IsNullOrWhiteSpace(evt.ActorType) ? "USER" : evt.ActorType,
                CreatedAt = evt.CreatedAt == default ? VietnamTime.Now() : evt.CreatedAt,
                IsRead = false
            };

            await notificationService.CreateNotificationAsync(entity);
            var createdEvent = await notificationService.BuildCreatedEventAsync(entity);
            if (!string.IsNullOrWhiteSpace(evt.EventId))
            {
                createdEvent.EventId = evt.EventId;
            }

            if (cache != null)
            {
                await cache.RemoveAsync($"unread:{recipientUserId}");
            }

            await eventPublisher.PublishNotificationCreatedAsync(createdEvent);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is not null) await _channel.CloseAsync(cancellationToken);
        if (_connection is not null) await _connection.CloseAsync(cancellationToken);
        await base.StopAsync(cancellationToken);
    }
}

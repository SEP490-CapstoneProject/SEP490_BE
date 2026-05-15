using System.Text;
using System.Text.Json;
using Connection.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RecruitmentPlatform.Contracts.Realtime;

namespace Connection.Infrastructure.Messaging;

/// <summary>
/// Publish connection/message events to RabbitMQ exchange "skillsnap.events".
/// Notification service consumes the chat payload and fans it out to realtime/FCM.
/// The Realtime Service consumes these and pushes via SignalR to end clients.
/// Each method opens a short-lived connection (fire-and-forget, non-critical path).
/// </summary>
public class RabbitMqConnectionEventPublisher : IConnectionEventPublisher
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqConnectionEventPublisher> _logger;

    private const string Exchange = "skillsnap.events";

    public RabbitMqConnectionEventPublisher(
        IConfiguration configuration,
        ILogger<RabbitMqConnectionEventPublisher> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    private ConnectionFactory CreateFactory()
    {
        var uri = _configuration["RabbitMQ:Uri"];
        var factory = new ConnectionFactory();

        if (!string.IsNullOrEmpty(uri))
        {
            factory.Uri = new Uri(uri);
        }
        else
        {
            var host = _configuration["RabbitMQ:HostName"] ?? _configuration["RabbitMQ:Host"] ?? "localhost";
            var userName = _configuration["RabbitMQ:UserName"] ?? _configuration["RabbitMQ:Username"] ?? "guest";
            factory.HostName = host;
            factory.UserName = userName;
            factory.Password = _configuration["RabbitMQ:Password"] ?? "guest";
            factory.VirtualHost = _configuration["RabbitMQ:VirtualHost"] ?? "/";
            factory.Port = int.TryParse(_configuration["RabbitMQ:Port"], out var port) ? port : 5672;
        }

        return factory;
    }

    private async Task PublishAsync<T>(string routingKey, T payload, CancellationToken cancellationToken) where T : RealtimeEventBase
    {
        try
        {
            await using var connection = await CreateFactory().CreateConnectionAsync(cancellationToken);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
            await channel.ExchangeDeclareAsync(Exchange, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken);

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
            await channel.BasicPublishAsync(Exchange, routingKey, body, cancellationToken);

            _logger.LogInformation(
                "Published {EventType} to {RoutingKey}. EventId={EventId}",
                typeof(T).Name, routingKey, payload.EventId);
        }
        catch (Exception ex)
        {
            // Log but don't crash the main flow — realtime notification is best-effort
            _logger.LogWarning(ex,
                "Failed to publish {EventType} to RabbitMQ. RoutingKey={RoutingKey}",
                typeof(T).Name, routingKey);
        }
    }

    private async Task PublishNotificationAsync(string routingKey, ConnectionNotificationEventPayload payload, CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = await CreateFactory().CreateConnectionAsync(cancellationToken);
            await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);
            await channel.ExchangeDeclareAsync(Exchange, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken);

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
            await channel.BasicPublishAsync(Exchange, routingKey, body, cancellationToken);

            _logger.LogInformation(
                "Published notification event {EventType} to {RoutingKey}. UserId={UserId}",
                payload.EventType, routingKey, payload.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Failed to publish notification event {EventType}. RoutingKey={RoutingKey}",
                payload.EventType, routingKey);
        }
    }

    public Task PublishConnectionRequestedAsync(
        int connectionId, int fromUserId, int toUserId, int profileId,
        DateTime requestedAt, CancellationToken cancellationToken = default)
    {
        var evt = new ConnectionRequestedEvent
        {
            EventType = "connection.requested",
            ConnectionId = connectionId,
            FromUserId = fromUserId,
            ToUserId = toUserId,
            ProfileId = profileId,
            RequestedAt = requestedAt,
            ActorId = fromUserId.ToString(),
            ActorType = "USER",
            CreatedAt = DateTime.UtcNow
        };
        return PublishAsync("connection.requested", evt, cancellationToken);
    }

    public Task PublishConnectionAcceptedAsync(
        int connectionId, int fromUserId, int toUserId,
        DateTime acceptedAt, CancellationToken cancellationToken = default)
    {
        var evt = new ConnectionAcceptedEvent
        {
            EventType = "connection.accepted",
            ConnectionId = connectionId,
            FromUserId = fromUserId,
            ToUserId = toUserId,
            AcceptedAt = acceptedAt,
            ActorId = toUserId.ToString(),
            ActorType = "USER",
            CreatedAt = DateTime.UtcNow
        };
        return PublishAsync("connection.accepted", evt, cancellationToken);
    }

    public Task PublishChatMessageNotificationAsync(ConnectionNotificationEventPayload payload, CancellationToken cancellationToken = default)
        => PublishNotificationAsync("connection.message.created", payload, cancellationToken);
        
    public Task PublishNewMessageNotificationAsync(
        int messageId, int roomId, int fromUserId, int toUserId,
        string content, DateTime sentAt, CancellationToken cancellationToken = default)
    {
        var evt = new NewMessageNotificationEvent
        {
            EventType = "message.new",
            MessageId = messageId,
            RoomId = roomId,
            FromUserId = fromUserId,
            ToUserId = toUserId,
            Content = content,
            SentAt = sentAt,
            ActorId = fromUserId.ToString(),
            ActorType = "USER",
            CreatedAt = DateTime.UtcNow
        };
        return PublishAsync("message.new", evt, cancellationToken);
    }

    public Task PublishConnectionRequestNotificationAsync(ConnectionNotificationEventPayload payload, CancellationToken cancellationToken = default)
        => PublishNotificationAsync("connection.request.created", payload, cancellationToken);

    public Task PublishConnectionAcceptedNotificationAsync(ConnectionNotificationEventPayload payload, CancellationToken cancellationToken = default)
        => PublishNotificationAsync("connection.request.accepted", payload, cancellationToken);
}

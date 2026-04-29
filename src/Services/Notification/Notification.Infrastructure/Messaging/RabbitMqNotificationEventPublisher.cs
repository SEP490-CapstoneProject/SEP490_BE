using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Notification.Application.DTOs;
using RabbitMQ.Client;
using RecruitmentPlatform.Contracts.Realtime;

namespace Notification.Infrastructure.Messaging;

public class RabbitMqNotificationEventPublisher : INotificationEventPublisher
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqNotificationEventPublisher> _logger;

    public RabbitMqNotificationEventPublisher(
        IConfiguration configuration,
        ILogger<RabbitMqNotificationEventPublisher> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task PublishNotificationCreatedAsync(NotificationCreatedEventDto evt, CancellationToken cancellationToken = default)
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

        await using var connection = await factory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        const string exchange = "skillsnap.events";
        const string routingKey = "notification.created";

        await channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken);

        var message = new NotificationCreatedEvent
        {
            EventId = evt.EventId,
            EventType = evt.EventType,
            Version = evt.Version,
            NotificationId = evt.NotificationId,
            UserId = evt.UserId,
            Title = evt.Title,
            Content = evt.Content,
            Type = evt.Type,
            Category = evt.Category,
            ObjectId = evt.ObjectId,
            Actor = evt.Actor is null
                ? null
                : new NotificationActorDto
                {
                    Id = evt.Actor.Id,
                    Name = evt.Actor.Name,
                    Avatar = evt.Actor.Avatar,
                    Role = evt.Actor.Role
                },
            CreatedAt = evt.CreatedAt,
            IsRead = evt.IsRead
        };

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));
        await channel.BasicPublishAsync(exchange, routingKey, body, cancellationToken);

        _logger.LogInformation(
            "Published notification.created event. EventId={EventId}, NotificationId={NotificationId}, UserId={UserId}",
            evt.EventId, evt.NotificationId, evt.UserId);
    }
}

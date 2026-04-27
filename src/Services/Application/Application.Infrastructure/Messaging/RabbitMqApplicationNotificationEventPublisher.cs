using System.Text;
using System.Text.Json;
using Application.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Application.Infrastructure.Messaging;

public class RabbitMqApplicationNotificationEventPublisher : IApplicationNotificationEventPublisher
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqApplicationNotificationEventPublisher> _logger;

    public RabbitMqApplicationNotificationEventPublisher(
        IConfiguration configuration,
        ILogger<RabbitMqApplicationNotificationEventPublisher> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task PublishAsync(ApplicationNotificationEventPayload payload, CancellationToken cancellationToken = default)
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:HostName"] ?? _configuration["RabbitMQ:Host"] ?? "localhost",
            UserName = _configuration["RabbitMQ:UserName"] ?? _configuration["RabbitMQ:Username"] ?? "guest",
            Password = _configuration["RabbitMQ:Password"] ?? "guest",
            VirtualHost = _configuration["RabbitMQ:VirtualHost"] ?? "/",
            Port = int.TryParse(_configuration["RabbitMQ:Port"], out var port) ? port : 5672
        };

        await using var connection = await factory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        const string exchange = "skillsnap.events";
        const string routingKeyCreated = "job.application.created";
        const string routingKeyStatusUpdated = "job.application.status.updated";

        await channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken);

        var routingKey = payload.EventType switch
        {
            "job.application.created" => routingKeyCreated,
            "job.application.status.updated" => routingKeyStatusUpdated,
            _ => payload.EventType
        };

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
        await channel.BasicPublishAsync(exchange, routingKey, body, cancellationToken);

        _logger.LogInformation(
            "Published {EventType} notification event. UserId={UserId}, ObjectId={ObjectId}",
            payload.EventType, payload.UserId, payload.ObjectId);
    }
}

using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Portfolio.Application.Interfaces;
using RabbitMQ.Client;

namespace Portfolio.Infrastructure.Messaging;

public class PortfolioNotificationEventPublisher : IPortfolioNotificationEventPublisher
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PortfolioNotificationEventPublisher> _logger;

    public PortfolioNotificationEventPublisher(
        IConfiguration configuration,
        ILogger<PortfolioNotificationEventPublisher> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task PublishComplimentCreatedAsync(PortfolioNotificationEventPayload payload, CancellationToken cancellationToken = default)
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
        const string routingKey = "portfolio.compliment.created";

        await channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
        await channel.BasicPublishAsync(exchange, routingKey, body, cancellationToken);

        _logger.LogInformation(
            "Published portfolio compliment notification. UserId={UserId}, ObjectId={ObjectId}",
            payload.UserId, payload.ObjectId);
    }
}

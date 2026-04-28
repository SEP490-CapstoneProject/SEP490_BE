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
        var uri = _configuration["RabbitMQ:Uri"];
        var factory = new ConnectionFactory();

        if (!string.IsNullOrEmpty(uri))
        {
            factory.Uri = new Uri(uri);
        }
        else
        {
            var host = GetRabbitSetting("Host", "HostName", "localhost");
            var userName = GetRabbitSetting("Username", "UserName", "guest");
            var virtualHost = GetRabbitSetting("VirtualHost", defaultValue: "/");
            var portValue = GetRabbitSetting("Port", defaultValue: "5672");

            factory.HostName = host;
            factory.UserName = userName;
            factory.Password = GetRabbitSetting("Password", defaultValue: "guest");
            factory.VirtualHost = virtualHost;
            factory.Port = int.TryParse(portValue, out var port) ? port : 5672;

            _logger.LogInformation(
                "Publishing portfolio compliment notification with RabbitMQ host {Host}, port {Port}, vhost {VHost}",
                host, factory.Port, virtualHost);
        }

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

    private string GetRabbitSetting(string primaryKey, string? fallbackKey = null, string defaultValue = "")
    {
        var primary = _configuration[$"RabbitMQ:{primaryKey}"];
        if (!string.IsNullOrWhiteSpace(primary))
        {
            return primary;
        }

        if (!string.IsNullOrWhiteSpace(fallbackKey))
        {
            var fallback = _configuration[$"RabbitMQ:{fallbackKey}"];
            if (!string.IsNullOrWhiteSpace(fallback))
            {
                return fallback;
            }
        }

        return defaultValue;
    }
}

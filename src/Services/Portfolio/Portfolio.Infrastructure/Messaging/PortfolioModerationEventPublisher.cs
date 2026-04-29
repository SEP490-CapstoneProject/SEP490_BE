using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Portfolio.Application.Interfaces;
using Portfolio.Application.Models.Events;
using RabbitMQ.Client;

namespace Portfolio.Infrastructure.Messaging;

public class PortfolioModerationEventPublisher : IPortfolioModerationEventPublisher
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PortfolioModerationEventPublisher> _logger;

    public PortfolioModerationEventPublisher(
        IConfiguration configuration,
        ILogger<PortfolioModerationEventPublisher> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task PublishPortfolioApprovedNotificationAsync(PortfolioApprovedNotificationEvent evt, CancellationToken cancellationToken = default)
    {
        await PublishEventAsync("portfolio.approved", evt, cancellationToken);
    }

    public async Task PublishPortfolioRejectedNotificationAsync(PortfolioRejectedNotificationEvent evt, CancellationToken cancellationToken = default)
    {
        await PublishEventAsync("portfolio.rejected", evt, cancellationToken);
    }

    public async Task PublishPortfolioPendingReviewNotificationAsync(PortfolioPendingReviewNotificationEvent evt, CancellationToken cancellationToken = default)
    {
        await PublishEventAsync("portfolio.pending.review", evt, cancellationToken);
    }

    public async Task PublishPortfolioModerationEventAsync(object evt, CancellationToken cancellationToken = default)
    {
        await PublishEventAsync("portfolio.moderation", evt, cancellationToken);
    }

    private async Task PublishEventAsync(string routingKey, object evt, CancellationToken cancellationToken)
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
        }

        await using var connection = await factory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        const string exchange = "skillsnap.events";

        await channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evt));
        await channel.BasicPublishAsync(exchange, routingKey, body, cancellationToken);

        _logger.LogInformation(
            "Published portfolio moderation event. RoutingKey={RoutingKey}, EventType={EventType}",
            routingKey, evt.GetType().Name);
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

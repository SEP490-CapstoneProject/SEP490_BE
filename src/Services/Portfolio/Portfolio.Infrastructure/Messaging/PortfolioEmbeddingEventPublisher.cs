using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Portfolio.Application.Interfaces;
using RabbitMQ.Client;

namespace Portfolio.Infrastructure.Messaging;

public sealed class PortfolioEmbeddingEventPublisher : IPortfolioEmbeddingEventPublisher
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PortfolioEmbeddingEventPublisher> _logger;

    public PortfolioEmbeddingEventPublisher(IConfiguration configuration, ILogger<PortfolioEmbeddingEventPublisher> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task PublishPortfolioChangedAsync(int portfolioId, CancellationToken cancellationToken = default)
    {
        var host = GetRabbitSetting("Host", "HostName", "localhost");
        var userName = GetRabbitSetting("Username", "UserName", "guest");
        var factory = new ConnectionFactory
        {
            HostName = host,
            UserName = userName,
            Password = GetRabbitSetting("Password", defaultValue: "guest"),
            VirtualHost = GetRabbitSetting("VirtualHost", defaultValue: "/"),
            Port = int.TryParse(GetRabbitSetting("Port", defaultValue: "5672"), out var port) ? port : 5672
        };

        await using var connection = await factory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        const string exchange = "skillsnap.events";
        await channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken);

        var payload = JsonSerializer.Serialize(new { PortfolioId = portfolioId });
        var body = Encoding.UTF8.GetBytes(payload);
        await channel.BasicPublishAsync(exchange, "portfolio.changed", body, cancellationToken);
        _logger.LogInformation("Published portfolio.changed for {PortfolioId}", portfolioId);
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

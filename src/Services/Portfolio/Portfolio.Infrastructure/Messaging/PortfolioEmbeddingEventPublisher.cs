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
        var host = _configuration["RabbitMQ:HostName"] ?? _configuration["RabbitMQ:Host"] ?? "localhost";
        var userName = _configuration["RabbitMQ:UserName"] ?? _configuration["RabbitMQ:Username"] ?? "guest";
        var factory = new ConnectionFactory
        {
            HostName = host,
            UserName = userName,
            Password = _configuration["RabbitMQ:Password"] ?? "guest",
            VirtualHost = _configuration["RabbitMQ:VirtualHost"] ?? "/",
            Port = int.TryParse(_configuration["RabbitMQ:Port"], out var port) ? port : 5672
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
}

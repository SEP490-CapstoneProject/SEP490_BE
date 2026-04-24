using System.Text;
using System.Text.Json;
using Company.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Company.Infrastructure.Messaging;

public sealed class CompanyEmbeddingEventPublisher : ICompanyEmbeddingEventPublisher
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<CompanyEmbeddingEventPublisher> _logger;

    public CompanyEmbeddingEventPublisher(IConfiguration configuration, ILogger<CompanyEmbeddingEventPublisher> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task PublishCompanyPostChangedAsync(int postId, CancellationToken cancellationToken = default)
    {
        var host = GetRabbitSetting("HostName", "Host", "localhost");
        var userName = GetRabbitSetting("UserName", "Username", "guest");
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

        var payload = JsonSerializer.Serialize(new { PostId = postId });
        var body = Encoding.UTF8.GetBytes(payload);
        await channel.BasicPublishAsync(exchange, "company.post.changed", body, cancellationToken);
        _logger.LogInformation("Published company.post.changed for {PostId}", postId);
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

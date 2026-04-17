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

        var payload = JsonSerializer.Serialize(new { PostId = postId });
        var body = Encoding.UTF8.GetBytes(payload);
        await channel.BasicPublishAsync(exchange, "company.post.changed", body, cancellationToken);
        _logger.LogInformation("Published company.post.changed for {PostId}", postId);
    }
}

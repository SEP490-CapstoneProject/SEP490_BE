using System.Text;
using System.Text.Json;
using Community.Application.Interfaces;
using Community.Application.Models.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Community.Infrastructure.Services;

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

    public async Task PublishPostFavoriteNotificationAsync(PostFavoriteNotificationEvent evt, CancellationToken cancellationToken = default)
    {
        await PublishAsync("post.favorite", evt, cancellationToken);
    }

    public async Task PublishPostRemovedByModerationNotificationAsync(PostRemovedByModerationNotificationEvent evt, CancellationToken cancellationToken = default)
    {
        await PublishAsync("post.report.removed", evt, cancellationToken);
    }

    public async Task PublishPostReportCreatedNotificationAsync(PostReportCreatedNotificationEvent evt, CancellationToken cancellationToken = default)
    {
        await PublishAsync("post.report.created", evt, cancellationToken);
    }

    private async Task PublishAsync<T>(string routingKey, T evt, CancellationToken cancellationToken)
    {
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:HostName"] ?? _configuration["RabbitMQ:Host"] ?? "localhost",
                UserName = _configuration["RabbitMQ:UserName"] ?? _configuration["RabbitMQ:Username"] ?? "guest",
                Password = _configuration["RabbitMQ:Password"] ?? "guest",
                VirtualHost = _configuration["RabbitMQ:VirtualHost"] ?? "/",
                Port = int.TryParse(_configuration["RabbitMQ:Port"], out var port) ? port : 5672
            };

            using var connection = await factory.CreateConnectionAsync(cancellationToken);
            using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

            const string exchange = "skillsnap.events";
            await channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken);

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evt, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));

            await channel.BasicPublishAsync(exchange, routingKey, body, cancellationToken);

            _logger.LogInformation("Published notification event {EventType} with routing key {RoutingKey}", 
                typeof(T).Name, routingKey);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish notification event {EventType} with routing key {RoutingKey}", 
                typeof(T).Name, routingKey);
            throw;
        }
    }
}

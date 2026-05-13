using System.Text;
using System.Text.Json;
using Company.Application.Interfaces;
using Company.Application.Models.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Company.Infrastructure.Messaging;

public class RabbitMqCompanyNotificationEventPublisher : ICompanyNotificationEventPublisher
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqCompanyNotificationEventPublisher> _logger;

    public RabbitMqCompanyNotificationEventPublisher(
        IConfiguration configuration,
        ILogger<RabbitMqCompanyNotificationEventPublisher> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task PublishPostApprovedNotificationAsync(PostApprovedNotificationEvent evt, CancellationToken cancellationToken = default)
    {
        await PublishAsync("post.approved", evt, cancellationToken);
    }

    public async Task PublishPostRejectedNotificationAsync(PostRejectedNotificationEvent evt, CancellationToken cancellationToken = default)
    {
        await PublishAsync("post.rejected", evt, cancellationToken);
    }

    public async Task PublishPostPendingReviewNotificationAsync(PostPendingReviewNotificationEvent evt, CancellationToken cancellationToken = default)
    {
        await PublishAsync("post.pending.review", evt, cancellationToken);
    }

    private async Task PublishAsync<T>(string routingKey, T evt, CancellationToken cancellationToken)
    {
        try
        {
            var uri = _configuration["RabbitMQ:Uri"];
            var factory = new ConnectionFactory();

            if (!string.IsNullOrEmpty(uri))
            {
                factory.Uri = new Uri(uri);
            }
            else
            {
                factory.HostName = _configuration["RabbitMQ:HostName"] ?? _configuration["RabbitMQ:Host"] ?? "localhost";
                factory.UserName = _configuration["RabbitMQ:UserName"] ?? _configuration["RabbitMQ:Username"] ?? "guest";
                factory.Password = _configuration["RabbitMQ:Password"] ?? "guest";
                factory.VirtualHost = _configuration["RabbitMQ:VirtualHost"] ?? "/";
                factory.Port = int.TryParse(_configuration["RabbitMQ:Port"], out var port) ? port : 5672;
            }

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

using System.Text;
using System.Text.Json;
using Community.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RecruitmentPlatform.Contracts.Realtime;

namespace Community.Infrastructure.Services;

public class RabbitMqCommunityEventPublisher : ICommunityEventPublisher
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<RabbitMqCommunityEventPublisher> _logger;

    public RabbitMqCommunityEventPublisher(
        IConfiguration configuration,
        ILogger<RabbitMqCommunityEventPublisher> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task PublishCommentCreatedAsync(CommentCreatedEvent evt, CancellationToken cancellationToken = default)
        => PublishAsync("post.comment.created", evt, cancellationToken);

    public Task PublishReplyCreatedAsync(ReplyCreatedEvent evt, CancellationToken cancellationToken = default)
        => PublishAsync("post.reply.created", evt, cancellationToken);

    public Task PublishPostFavoriteChangedAsync(PostFavoriteChangedEvent evt, CancellationToken cancellationToken = default)
        => PublishAsync("post.favorite.changed", evt, cancellationToken);

    private async Task PublishAsync<T>(string routingKey, T evt, CancellationToken cancellationToken)
    {
        var uri = _configuration["RabbitMQ:Uri"];
        var factory = new ConnectionFactory();

        if (!string.IsNullOrEmpty(uri))
        {
            factory.Uri = new Uri(uri);
        }
        else
        {
            var host = _configuration["RabbitMQ:HostName"] ?? _configuration["RabbitMQ:Host"] ?? "localhost";
            var userName = _configuration["RabbitMQ:UserName"] ?? _configuration["RabbitMQ:Username"] ?? "guest";
            factory.HostName = host;
            factory.UserName = userName;
            factory.Password = _configuration["RabbitMQ:Password"] ?? "guest";
            factory.VirtualHost = _configuration["RabbitMQ:VirtualHost"] ?? "/";
            factory.Port = int.TryParse(_configuration["RabbitMQ:Port"], out var port) ? port : 5672;
        }

        await using var connection = await factory.CreateConnectionAsync(cancellationToken);
        await using var channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        const string exchange = "skillsnap.events";
        await channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true, cancellationToken: cancellationToken);

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evt));
        await channel.BasicPublishAsync(exchange, routingKey, body, cancellationToken);

        _logger.LogInformation("Published realtime event {RoutingKey}", routingKey);
    }
}

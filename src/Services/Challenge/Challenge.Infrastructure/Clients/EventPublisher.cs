using System.Text;
using System.Text.Json;
using Challenge.Application.Clients;
using Challenge.Application.Models.Events;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Challenge.Infrastructure.Clients;

public class EventPublisher : IEventPublisher
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EventPublisher> _logger;

    public EventPublisher(IConfiguration configuration, ILogger<EventPublisher> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task PublishChallengeCreatedAsync(int challengeId, string title, int createdById)
        => PublishAsync("challenge.created", new ChallengeCreatedEvent
        {
            ChallengeId = challengeId,
            Title = title,
            CreatedById = createdById
        });

    public Task PublishChallengePublishedAsync(int challengeId, string title)
        => PublishAsync("challenge.published", new ChallengePublishedEvent
        {
            ChallengeId = challengeId,
            Title = title
        });

    public Task PublishSubmissionGradedAsync(int submissionId, int userId, int challengeId, double score)
        => PublishAsync("submission.graded", new SubmissionGradedEvent
        {
            SubmissionId = submissionId,
            UserId = userId,
            ChallengeId = challengeId,
            OverallScore = score
        });

    public Task PublishSkillPointsAwardedAsync(int userId, Dictionary<int, double> skillPoints, int challengeId)
        => PublishAsync("skillpoints.awarded", new SkillPointsAwardedEvent
        {
            UserId = userId,
            ChallengeId = challengeId,
            SkillPoints = skillPoints
        });

    private async Task PublishAsync<T>(string routingKey, T evt)
    {
        var uri = _configuration["RabbitMQ:Uri"];
        var factory = new ConnectionFactory();

        if (!string.IsNullOrWhiteSpace(uri))
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

        await using var connection = await factory.CreateConnectionAsync();
        await using var channel = await connection.CreateChannelAsync();

        const string exchange = "skillsnap.events";
        await channel.ExchangeDeclareAsync(exchange, ExchangeType.Topic, durable: true);

        var payload = new
        {
            eventType = routingKey,
            data = evt,
            timestamp = DateTime.UtcNow
        };

        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
        await channel.BasicPublishAsync(exchange, routingKey, body);

        _logger.LogInformation("Published challenge event {RoutingKey}", routingKey);
    }
}

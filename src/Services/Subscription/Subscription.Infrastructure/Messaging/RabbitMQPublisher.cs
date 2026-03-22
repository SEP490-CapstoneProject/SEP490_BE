using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Subscription.Application.Events;
using Subscription.Application.Interfaces;
using Subscription.Domain.Entities;
using Subscription.Domain.Enums;

namespace Subscription.Infrastructure.Messaging;

public class RabbitMQPublisher : IRabbitMQPublisher
{
    private readonly IConnection _connection;
    private readonly IOutboxRepository _outboxRepository;
    private readonly ILogger<RabbitMQPublisher> _logger;
    private const string ExchangeName = "subscription_events";

    public RabbitMQPublisher(
        IConnection connection,
        IOutboxRepository outboxRepository,
        ILogger<RabbitMQPublisher> logger)
    {
        _connection = connection;
        _outboxRepository = outboxRepository;
        _logger = logger;
    }

    public async Task PublishAsync<T>(T @event) where T : BaseEvent
    {
        try
        {
            using var channel = _connection.CreateModel();
            
            channel.ExchangeDeclare(
                exchange: ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            var json = JsonSerializer.Serialize(@event, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var body = Encoding.UTF8.GetBytes(json);

            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.MessageId = @event.EventId;
            properties.CorrelationId = @event.CorrelationId;
            properties.Type = @event.EventType;
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            channel.BasicPublish(
                exchange: ExchangeName,
                routingKey: @event.EventType,
                basicProperties: properties,
                body: body);

            _logger.LogInformation("Published event {EventType} with ID {EventId}, CorrelationId {CorrelationId}", 
                @event.EventType, @event.EventId, @event.CorrelationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event {EventId}, storing in outbox", @event.EventId);
            
            // Outbox pattern: store for later retry
            await _outboxRepository.AddAsync(new OutboxEvent
            {
                EventId = @event.EventId,
                EventType = @event.EventType,
                Payload = JsonSerializer.Serialize(@event, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                }),
                Status = OutboxStatus.Pending
            });
        }
    }

    public Task<bool> IsHealthyAsync()
    {
        return Task.FromResult(_connection.IsOpen);
    }
}

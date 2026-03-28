using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Payment.Application.Events;
using Payment.Application.Interfaces;
using RabbitMQ.Client;

namespace Payment.Infrastructure.Services;

public class RabbitMqPaymentEventPublisher : IPaymentEventPublisher
{
    private readonly IConnection _connection;
    private readonly ILogger<RabbitMqPaymentEventPublisher> _logger;
    private const string ExchangeName = "skillsnap.events";
    private const string DeadLetterExchange = "skillsnap.dlx";
    private const string DeadLetterQueue = "payment.failed.dlq";

    public RabbitMqPaymentEventPublisher(IConnection connection, ILogger<RabbitMqPaymentEventPublisher> logger)
    {
        _connection = connection;
        _logger = logger;
        
        // Initialize exchanges and DLQ on startup
        InitializeInfrastructure();
    }

    private void InitializeInfrastructure()
    {
        try
        {
            using var channel = _connection.CreateModel();

            // Dead Letter Exchange (DLX)
            channel.ExchangeDeclare(
                exchange: DeadLetterExchange,
                type: ExchangeType.Direct,
                durable: true,
                autoDelete: false);

            // Dead Letter Queue
            channel.QueueDeclare(
                queue: DeadLetterQueue,
                durable: true,
                exclusive: false,
                autoDelete: false);

            channel.QueueBind(
                queue: DeadLetterQueue,
                exchange: DeadLetterExchange,
                routingKey: "payment.failed");

            // Main exchange with DLX configuration
            channel.ExchangeDeclare(
                exchange: ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false);

            // Main queue with DLQ routing
            var args = new Dictionary<string, object>
            {
                { "x-dead-letter-exchange", DeadLetterExchange },
                { "x-dead-letter-routing-key", "payment.failed" },
                { "x-message-ttl", 86400000 } // 24 hour TTL
            };

            channel.QueueDeclare(
                queue: "payment.succeeded",
                durable: true,
                exclusive: false,
                autoDelete: false,
                arguments: args);

            channel.QueueBind(
                queue: "payment.succeeded",
                exchange: ExchangeName,
                routingKey: "payment.succeeded");

            _logger.LogInformation("RabbitMQ infrastructure initialized with DLQ support");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize RabbitMQ infrastructure");
        }
    }

    public async Task PublishPaymentSucceededAsync(Guid paymentId, int userId, int planId, int subscriptionId, decimal amount, string provider)
    {
        var paymentEvent = new PaymentSucceededEvent
        {
            EventId = $"payment_{paymentId}_{Guid.NewGuid()}",
            PaymentId = paymentId,
            UserId = userId,
            PlanId = planId,
            SubscriptionId = subscriptionId,
            Amount = amount,
            Provider = provider,
            Timestamp = DateTime.UtcNow
        };

        await PublishAsync(paymentEvent);
    }

    private Task PublishAsync(PaymentSucceededEvent paymentEvent)
    {
        try
        {
            using var channel = _connection.CreateModel();

            var message = JsonSerializer.Serialize(paymentEvent);
            var body = Encoding.UTF8.GetBytes(message);

            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";
            properties.MessageId = paymentEvent.EventId;
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            properties.Headers = new Dictionary<string, object>
            {
                { "x-retry-count", 0 }
            };

            channel.BasicPublish(
                exchange: ExchangeName,
                routingKey: "payment.succeeded",
                basicProperties: properties,
                body: body);

            _logger.LogInformation("Published payment.succeeded event. EventId: {EventId}, PaymentId: {PaymentId}, SubscriptionId: {SubscriptionId}",
                paymentEvent.EventId, paymentEvent.PaymentId, paymentEvent.SubscriptionId);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish payment.succeeded event. EventId: {EventId}", paymentEvent.EventId);
            throw;
        }
    }
}

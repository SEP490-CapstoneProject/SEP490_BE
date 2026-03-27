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
    private const string ExchangeName = "payment_events";

    public RabbitMqPaymentEventPublisher(IConnection connection, ILogger<RabbitMqPaymentEventPublisher> logger)
    {
        _connection = connection;
        _logger = logger;
    }

    public async Task PublishPaymentSucceededAsync(Guid paymentId, int userId, int planId, decimal amount, string provider)
    {
        var paymentEvent = new PaymentSucceededEvent
        {
            EventId = $"payment_{paymentId}_{Guid.NewGuid()}",
            PaymentId = paymentId,
            UserId = userId,
            PlanId = planId,
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

            channel.ExchangeDeclare(
                exchange: ExchangeName,
                type: ExchangeType.Topic,
                durable: true,
                autoDelete: false
            );

            var message = JsonSerializer.Serialize(paymentEvent);
            var body = Encoding.UTF8.GetBytes(message);

            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";
            properties.MessageId = paymentEvent.EventId;
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());

            channel.BasicPublish(
                exchange: ExchangeName,
                routingKey: "payment.succeeded",
                basicProperties: properties,
                body: body
            );

            _logger.LogInformation("Published payment.succeeded event: {EventId}, PaymentId: {PaymentId}",
                paymentEvent.EventId, paymentEvent.PaymentId);

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish payment.succeeded event: {EventId}", paymentEvent.EventId);
            throw;
        }
    }
}

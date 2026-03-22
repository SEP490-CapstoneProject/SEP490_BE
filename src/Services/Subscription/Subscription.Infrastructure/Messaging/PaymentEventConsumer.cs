using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Subscription.Application.Events;
using Subscription.Application.Interfaces;

namespace Subscription.Infrastructure.Messaging;

public class PaymentEventConsumer : BackgroundService
{
    private readonly IConnection _connection;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PaymentEventConsumer> _logger;
    private IModel? _channel;
    private const string QueueName = "subscription_payment_events";
    private const string ExchangeName = "payment_events";

    public PaymentEventConsumer(
        IConnection connection,
        IServiceProvider serviceProvider,
        ILogger<PaymentEventConsumer> logger)
    {
        _connection = connection;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _channel = _connection.CreateModel();
        
        _channel.ExchangeDeclare(
            exchange: ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        _channel.QueueDeclare(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false);

        _channel.QueueBind(
            queue: QueueName,
            exchange: ExchangeName,
            routingKey: "payment.succeeded");

        _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

        var consumer = new EventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            var body = ea.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);
            
            try
            {
                await ProcessPaymentEventAsync(message, ea);
                _channel.BasicAck(ea.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing payment event");
                _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
            }
        };

        _channel.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);
        
        _logger.LogInformation("Payment event consumer started, listening on queue: {Queue}", QueueName);
        
        return Task.CompletedTask;
    }

    private async Task ProcessPaymentEventAsync(string message, BasicDeliverEventArgs ea)
    {
        using var scope = _serviceProvider.CreateScope();
        var subscriptionService = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();
        var processedEventRepo = scope.ServiceProvider.GetRequiredService<IProcessedEventRepository>();
        var redisService = scope.ServiceProvider.GetRequiredService<IRedisService>();

        var paymentEvent = JsonSerializer.Deserialize<PaymentSucceededEvent>(message, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (paymentEvent == null)
        {
            _logger.LogWarning("Failed to deserialize payment event");
            return;
        }

        _logger.LogInformation("Processing payment event {EventId}, SubscriptionId: {SubscriptionId}", 
            paymentEvent.EventId, paymentEvent.SubscriptionId);

        // Idempotency check
        if (await processedEventRepo.ExistsAsync(paymentEvent.EventId))
        {
            _logger.LogWarning("Event {EventId} already processed, skipping", paymentEvent.EventId);
            return;
        }

        // Timestamp validation (out-of-order detection)
        var lastEventTime = await redisService.GetLastEventTimeAsync(paymentEvent.UserId);
        if (lastEventTime != null && paymentEvent.Timestamp < lastEventTime)
        {
            _logger.LogWarning("Stale event {EventId} received (timestamp: {Timestamp}, last: {Last})", 
                paymentEvent.EventId, paymentEvent.Timestamp, lastEventTime);
            return;
        }

        // Process the payment
        await subscriptionService.ActivateSubscriptionAsync(paymentEvent.SubscriptionId, paymentEvent.EventId);

        // Update last event time
        await redisService.SetLastEventTimeAsync(paymentEvent.UserId, paymentEvent.Timestamp);

        _logger.LogInformation("Successfully processed payment event {EventId}", paymentEvent.EventId);
    }

    public override void Dispose()
    {
        _channel?.Close();
        _channel?.Dispose();
        base.Dispose();
    }
}

using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Subscription.Application.Interfaces;
using Subscription.Domain.Enums;

namespace Subscription.Infrastructure.Messaging;

public class OutboxProcessorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnection _connection;
    private readonly ILogger<OutboxProcessorService> _logger;
    private const string ExchangeName = "subscription_events";
    private readonly TimeSpan _processingInterval = TimeSpan.FromSeconds(10);

    public OutboxProcessorService(
        IServiceProvider serviceProvider,
        IConnection connection,
        ILogger<OutboxProcessorService> logger)
    {
        _serviceProvider = serviceProvider;
        _connection = connection;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Outbox processor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingEventsAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox events");
            }

            await Task.Delay(_processingInterval, stoppingToken);
        }
    }

    private async Task ProcessPendingEventsAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

        var pendingEvents = await outboxRepository.GetPendingAsync(limit: 100);
        
        if (!pendingEvents.Any())
            return;

        _logger.LogInformation("Processing {Count} pending outbox events", pendingEvents.Count());

        using var channel = _connection.CreateModel();
        channel.ExchangeDeclare(
            exchange: ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        foreach (var @event in pendingEvents)
        {
            try
            {
                var body = Encoding.UTF8.GetBytes(@event.Payload);
                
                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;
                properties.MessageId = @event.EventId;
                properties.Type = @event.EventType;

                channel.BasicPublish(
                    exchange: ExchangeName,
                    routingKey: @event.EventType,
                    basicProperties: properties,
                    body: body);

                await outboxRepository.MarkAsPublishedAsync(@event.Id);
                _logger.LogInformation("Published outbox event {EventId}", @event.EventId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish outbox event {EventId}", @event.EventId);
                await outboxRepository.IncrementRetryCountAsync(@event.Id, ex.Message);
            }
        }
    }
}

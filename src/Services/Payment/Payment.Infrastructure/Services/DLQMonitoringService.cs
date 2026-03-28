using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Payment.Domain.Enums;
using Payment.Domain.Interfaces;
using RabbitMQ.Client;
using System.Text.Json;

namespace Payment.Infrastructure.Services;

/// <summary>
/// Background service that monitors the Dead Letter Queue (DLQ) for failed messages.
/// Retries eligible messages and alerts on permanently failed ones.
/// </summary>
public class DLQMonitoringService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnection _connection;
    private readonly ILogger<DLQMonitoringService> _logger;
    private const int CheckIntervalMinutes = 60;
    private const int MaxRetries = 3;

    public DLQMonitoringService(
        IServiceProvider serviceProvider, 
        IConnection connection,
        ILogger<DLQMonitoringService> logger)
    {
        _serviceProvider = serviceProvider;
        _connection = connection;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DLQMonitoringService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDLQAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DLQMonitoringService");
            }

            await Task.Delay(TimeSpan.FromMinutes(CheckIntervalMinutes), stoppingToken);
        }

        _logger.LogInformation("DLQMonitoringService stopped");
    }

    private async Task ProcessDLQAsync(CancellationToken cancellationToken)
    {
        using var channel = _connection.CreateModel();
        
        // Declare DLQ (ensure it exists)
        channel.QueueDeclare(
            queue: "payment.failed.dlq",
            durable: true,
            exclusive: false,
            autoDelete: false);

        var messageCount = channel.MessageCount("payment.failed.dlq");
        
        if (messageCount == 0)
        {
            _logger.LogDebug("DLQ is empty");
            return;
        }

        _logger.LogWarning("DLQ contains {Count} failed messages", messageCount);

        using var scope = _serviceProvider.CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxEventRepository>();

        // Process up to 100 messages per cycle
        for (int i = 0; i < Math.Min(messageCount, 100); i++)
        {
            if (cancellationToken.IsCancellationRequested) break;

            var result = channel.BasicGet("payment.failed.dlq", autoAck: false);
            if (result == null) break;

            try
            {
                var body = System.Text.Encoding.UTF8.GetString(result.Body.ToArray());
                var message = JsonSerializer.Deserialize<DLQMessage>(body);

                if (message == null)
                {
                    _logger.LogWarning("Could not deserialize DLQ message");
                    channel.BasicNack(result.DeliveryTag, multiple: false, requeue: false);
                    continue;
                }

                if (message.RetryCount >= MaxRetries)
                {
                    // Permanently failed - move to poison queue or alert
                    _logger.LogCritical("DLQ: Message {EventId} exceeded max retries ({Max}). Moving to poison queue.",
                        message.EventId, MaxRetries);
                    channel.BasicNack(result.DeliveryTag, multiple: false, requeue: false);
                    continue;
                }

                // Retry the message
                _logger.LogInformation("DLQ: Retrying message {EventId} (attempt {Attempt})",
                    message.EventId, message.RetryCount + 1);

                // Republish to main queue with incremented retry count
                message.RetryCount++;
                var newBody = JsonSerializer.SerializeToUtf8Bytes(message);
                
                var properties = channel.CreateBasicProperties();
                properties.Persistent = true;
                
                channel.BasicPublish(
                    exchange: "skillsnap.events",
                    routingKey: "payment.succeeded",
                    basicProperties: properties,
                    body: newBody);

                channel.BasicAck(result.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing DLQ message");
                channel.BasicNack(result.DeliveryTag, multiple: false, requeue: true);
            }
        }

        await Task.CompletedTask;
    }

    private class DLQMessage
    {
        public string? EventId { get; set; }
        public string? EventType { get; set; }
        public string? Payload { get; set; }
        public int RetryCount { get; set; }
    }
}

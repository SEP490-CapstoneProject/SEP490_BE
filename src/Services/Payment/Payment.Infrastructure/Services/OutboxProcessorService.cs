using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Payment.Application.Interfaces;
using Payment.Domain.Enums;
using Payment.Domain.Interfaces;

namespace Payment.Infrastructure.Services;

public class OutboxProcessorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OutboxProcessorService> _logger;
    private const int BatchSize = 10;
    private const int ProcessIntervalSeconds = 5;

    public OutboxProcessorService(IServiceProvider serviceProvider, ILogger<OutboxProcessorService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxProcessorService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingEventsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OutboxProcessorService");
            }

            await Task.Delay(TimeSpan.FromSeconds(ProcessIntervalSeconds), stoppingToken);
        }

        _logger.LogInformation("OutboxProcessorService stopped");
    }

    private async Task ProcessPendingEventsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxEventRepository>();
        var eventPublisher = scope.ServiceProvider.GetRequiredService<IPaymentEventPublisher>();

        var pendingEvents = await outboxRepository.GetPendingEventsAsync(BatchSize);
        if (pendingEvents.Count == 0) return;

        _logger.LogInformation("Processing {Count} outbox events", pendingEvents.Count);

        foreach (var outboxEvent in pendingEvents)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                // Parse payload and publish
                var payload = System.Text.Json.JsonDocument.Parse(outboxEvent.Payload);
                var root = payload.RootElement;

                await eventPublisher.PublishPaymentSucceededAsync(
                    root.GetProperty("paymentId").GetGuid(),
                    root.GetProperty("userId").GetInt32(),
                    root.GetProperty("planId").GetInt32(),
                    root.GetProperty("subscriptionId").GetInt32(),  // Added!
                    root.GetProperty("amount").GetDecimal(),
                    root.GetProperty("provider").GetString() ?? ""
                );

                // Mark as published
                await outboxRepository.UpdateStatusAsync(outboxEvent.Id, OutboxStatus.Published);

                _logger.LogInformation("Published outbox event {EventId}", outboxEvent.EventId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish outbox event {EventId}", outboxEvent.EventId);

                // Exponential backoff: 2^n * 10 seconds
                var delaySeconds = Math.Pow(2, outboxEvent.RetryCount) * 10;
                var nextRetry = DateTime.UtcNow.AddSeconds(delaySeconds);

                await outboxRepository.IncrementRetryAsync(
                    outboxEvent.Id,
                    nextRetry,
                    ex.Message
                );

                if (outboxEvent.RetryCount >= 4) // Will be 5 after increment
                {
                    _logger.LogWarning("Outbox event {EventId} moved to dead letter after 5 retries", 
                        outboxEvent.EventId);
                }
            }
        }
    }
}

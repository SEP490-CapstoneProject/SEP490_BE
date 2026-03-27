using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Payment.Domain.Enums;
using Payment.Domain.Interfaces;

namespace Payment.Infrastructure.Services;

public class PaymentExpirationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<PaymentExpirationService> _logger;
    private const int CheckIntervalMinutes = 1;

    public PaymentExpirationService(IServiceProvider serviceProvider, ILogger<PaymentExpirationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PaymentExpirationService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExpirePendingPaymentsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in PaymentExpirationService");
            }

            await Task.Delay(TimeSpan.FromMinutes(CheckIntervalMinutes), stoppingToken);
        }

        _logger.LogInformation("PaymentExpirationService stopped");
    }

    private async Task ExpirePendingPaymentsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var paymentRepository = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();

        var threshold = DateTime.UtcNow;
        var expiredPayments = await paymentRepository.GetExpiredPendingPaymentsAsync(threshold);

        if (expiredPayments.Count == 0) return;

        _logger.LogInformation("Found {Count} expired pending payments", expiredPayments.Count);

        var paymentIds = expiredPayments.Select(p => p.Id).ToList();
        await paymentRepository.BulkUpdateStatusAsync(paymentIds, PaymentStatus.Expired);

        _logger.LogInformation("Expired {Count} pending payments", paymentIds.Count);
    }
}

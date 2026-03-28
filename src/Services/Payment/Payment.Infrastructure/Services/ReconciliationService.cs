using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Payment.Application.Interfaces;
using Payment.Domain.Enums;
using Payment.Domain.Interfaces;

namespace Payment.Infrastructure.Services;

/// <summary>
/// Background service that reconciles stuck payments with PayOS API.
/// Runs every 5 minutes to detect and fix payments that got stuck in Processing state.
/// </summary>
public class ReconciliationService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReconciliationService> _logger;
    private const int CheckIntervalMinutes = 5;
    private const int StuckThresholdMinutes = 10;

    public ReconciliationService(IServiceProvider serviceProvider, ILogger<ReconciliationService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReconciliationService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ReconcileStuckPaymentsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ReconciliationService");
            }

            await Task.Delay(TimeSpan.FromMinutes(CheckIntervalMinutes), stoppingToken);
        }

        _logger.LogInformation("ReconciliationService stopped");
    }

    private async Task ReconcileStuckPaymentsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var paymentRepository = scope.ServiceProvider.GetRequiredService<IPaymentRepository>();
        var verificationService = scope.ServiceProvider.GetRequiredService<IPaymentVerificationService>();

        // Find payments stuck in Processing or Pending for too long
        var stuckStatuses = new[] { PaymentStatus.Processing, PaymentStatus.Pending };
        var stuckPayments = await paymentRepository.GetStuckPaymentsAsync(StuckThresholdMinutes, stuckStatuses);

        if (stuckPayments.Count == 0)
        {
            _logger.LogDebug("No stuck payments found");
            return;
        }

        _logger.LogInformation("Found {Count} stuck payments, reconciling...", stuckPayments.Count);

        foreach (var payment in stuckPayments)
        {
            if (cancellationToken.IsCancellationRequested) break;

            try
            {
                var verificationResult = await verificationService.VerifyByOrderCodeAsync(payment.OrderCode);

                if (verificationResult.IsValid && verificationResult.ActualStatus == "PAID")
                {
                    // Payment was actually successful - update to Succeeded
                    _logger.LogWarning("Reconciliation: Payment {Id} was stuck but PayOS says PAID. Fixing.",
                        payment.Id);

                    await paymentRepository.UpdateStatusAsync(payment.Id, PaymentStatus.Succeeded);
                }
                else if (verificationResult.IsValid && verificationResult.ActualStatus == "CANCELLED")
                {
                    // Payment was cancelled - update to Cancelled
                    _logger.LogInformation("Reconciliation: Payment {Id} cancelled by PayOS", payment.Id);
                    await paymentRepository.UpdateStatusAsync(payment.Id, PaymentStatus.Cancelled);
                }
                else if (verificationResult.IsValid && verificationResult.ActualStatus == "EXPIRED")
                {
                    // Payment expired - update to Expired
                    _logger.LogInformation("Reconciliation: Payment {Id} expired in PayOS", payment.Id);
                    await paymentRepository.UpdateStatusAsync(payment.Id, PaymentStatus.Expired);
                }
                else if (!verificationResult.IsValid)
                {
                    // Could not verify - log for manual review
                    _logger.LogWarning("Reconciliation: Could not verify payment {Id}. Reason: {Reason}",
                        payment.Id, verificationResult.FailureReason);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Reconciliation error for payment {Id}", payment.Id);
            }
        }

        _logger.LogInformation("Reconciliation cycle completed");
    }
}

using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Domain.Interfaces;
using Payment.Infrastructure.Data;

namespace Payment.Infrastructure.Services;

public class WebhookHandler : IWebhookHandler
{
    private readonly PaymentDbContext _context;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IPaymentHistoryRepository _historyRepository;
    private readonly IProcessedEventRepository _processedEventRepository;
    private readonly IOutboxEventRepository _outboxRepository;
    private readonly Dictionary<string, IPaymentProvider> _providers;
    private readonly ILogger<WebhookHandler> _logger;

    public WebhookHandler(
        PaymentDbContext context,
        IPaymentRepository paymentRepository,
        IPaymentHistoryRepository historyRepository,
        IProcessedEventRepository processedEventRepository,
        IOutboxEventRepository outboxRepository,
        IEnumerable<IPaymentProvider> providers,
        ILogger<WebhookHandler> logger)
    {
        _context = context;
        _paymentRepository = paymentRepository;
        _historyRepository = historyRepository;
        _processedEventRepository = processedEventRepository;
        _outboxRepository = outboxRepository;
        _logger = logger;

        // Map providers by index (VNPay first, MoMo second in DI registration)
        var providerList = providers.ToList();
        _providers = new Dictionary<string, IPaymentProvider>();
        if (providerList.Count >= 1)
            _providers["vnpay"] = providerList[0];
        if (providerList.Count >= 2)
            _providers["momo"] = providerList[1];
    }

    public async Task<bool> HandleWebhookAsync(string providerName, IDictionary<string, string> queryParams, string correlationId)
    {
        // 1. Get provider
        if (!_providers.TryGetValue(providerName.ToLower(), out var provider))
        {
            _logger.LogError("Unknown provider: {Provider}", providerName);
            return false;
        }

        // 2. Validate signature (MANDATORY)
        if (!provider.ValidateSignature(queryParams))
        {
            _logger.LogWarning("Invalid signature from {Provider}. CorrelationId: {CorrelationId}", 
                providerName, correlationId);
            return false;
        }

        // 3. Parse webhook data
        var webhookResult = provider.ParseWebhookData(queryParams);
        if (!webhookResult.IsValid || string.IsNullOrEmpty(webhookResult.OrderCode))
        {
            _logger.LogWarning("Invalid webhook data from {Provider}. CorrelationId: {CorrelationId}", 
                providerName, correlationId);
            return false;
        }

        // 4. Hash-based idempotency check
        var rawHash = ComputeSha256Hash(webhookResult.RawData ?? "");
        if (await _processedEventRepository.IsProcessedByHashAsync(rawHash))
        {
            _logger.LogInformation("Webhook already processed (hash: {Hash}). CorrelationId: {CorrelationId}", 
                rawHash, correlationId);
            return true; // Already processed, return success
        }

        // 5. Find payment
        var payment = await _paymentRepository.GetByOrderCodeAsync(webhookResult.OrderCode);
        if (payment == null)
        {
            _logger.LogError("Payment not found for OrderCode: {OrderCode}. CorrelationId: {CorrelationId}", 
                webhookResult.OrderCode, correlationId);
            return false;
        }

        // 6. Anti-fraud validation
        if (Math.Abs(payment.Amount - webhookResult.Amount) > 0.01m)
        {
            _logger.LogError("Amount mismatch! Expected: {Expected}, Received: {Received}. PaymentId: {PaymentId}", 
                payment.Amount, webhookResult.Amount, payment.Id);
            return false;
        }

        if (payment.Currency != webhookResult.Currency)
        {
            _logger.LogError("Currency mismatch! Expected: {Expected}, Received: {Received}. PaymentId: {PaymentId}", 
                payment.Currency, webhookResult.Currency, payment.Id);
            return false;
        }

        // 7. TRANSACTIONAL WEBHOOK HANDLING (ALL 11 STEPS IN ONE TRANSACTION)
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var oldStatus = payment.Status;
            var newStatus = webhookResult.IsSuccess ? PaymentStatus.Success : PaymentStatus.Failed;

            // Step 7.1: Update payment status with conditional update (RowVersion check)
            var updated = await _paymentRepository.UpdateStatusConditionalAsync(
                payment.Id,
                newStatus,
                payment.RowVersion,
                webhookResult.TransactionId,
                webhookResult.IsSuccess ? DateTime.UtcNow : null
            );

            if (!updated)
            {
                _logger.LogWarning("Payment {PaymentId} already updated (race condition prevented)", payment.Id);
                await transaction.RollbackAsync();
                return true; // Already handled by another request
            }

            // Step 7.2: Save PaymentHistory
            var history = new PaymentHistory
            {
                Id = Guid.NewGuid(),
                PaymentId = payment.Id,
                OldStatus = oldStatus.ToString(),
                NewStatus = newStatus.ToString(),
                Action = "WebhookReceived",
                RawData = webhookResult.RawData,
                CorrelationId = correlationId,
                CreatedAt = DateTime.UtcNow
            };
            await _historyRepository.CreateAsync(history);

            // Step 7.3: Mark event as processed
            var processedEvent = new ProcessedEvent
            {
                EventId = rawHash,
                EventType = $"webhook.{providerName}",
                RawHash = rawHash,
                ProcessedAt = DateTime.UtcNow
            };
            await _processedEventRepository.MarkAsProcessedAsync(processedEvent);

            // Step 7.4: Publish event to outbox (only if success)
            if (webhookResult.IsSuccess)
            {
                var outboxEvent = new OutboxEvent
                {
                    EventId = $"payment_{payment.Id}_{Guid.NewGuid()}",
                    EventType = "payment.succeeded",
                    Payload = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        eventType = "payment.succeeded",
                        paymentId = payment.Id,
                        userId = payment.UserId,
                        planId = payment.PlanId,
                        subscriptionId = payment.SubscriptionId,  // Added!
                        amount = payment.Amount,
                        provider = payment.Provider.ToString(),
                        timestamp = DateTime.UtcNow
                    }),
                    Status = OutboxStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                };
                await _outboxRepository.CreateAsync(outboxEvent);
            }

            // Commit transaction
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Webhook processed successfully. PaymentId: {PaymentId}, Status: {Status}, CorrelationId: {CorrelationId}",
                payment.Id, newStatus, correlationId);

            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error processing webhook for PaymentId: {PaymentId}, CorrelationId: {CorrelationId}", 
                payment.Id, correlationId);
            return false;
        }
    }

    private static string ComputeSha256Hash(string rawString)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(rawString);
        var hash = sha256.ComputeHash(bytes);
        return BitConverter.ToString(hash).Replace("-", "").ToLower();
    }
}

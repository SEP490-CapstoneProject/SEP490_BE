using Microsoft.Extensions.Logging;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Domain.Interfaces;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Payment.Application.Services;

/// <summary>
/// Handles webhook processing with full transaction safety, idempotency, and verification
/// </summary>
public class WebhookService : IWebhookService
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IProcessedEventRepository _processedEventRepository;
    private readonly IPaymentHistoryRepository _historyRepository;
    private readonly IOutboxEventRepository _outboxRepository;
    private readonly IPaymentProvider _paymentProvider;
    private readonly IPaymentVerificationService _verificationService;
    private readonly ILogger<WebhookService> _logger;

    public WebhookService(
        IPaymentRepository paymentRepository,
        IProcessedEventRepository processedEventRepository,
        IPaymentHistoryRepository historyRepository,
        IOutboxEventRepository outboxRepository,
        IPaymentProvider paymentProvider,
        IPaymentVerificationService verificationService,
        ILogger<WebhookService> logger)
    {
        _paymentRepository = paymentRepository;
        _processedEventRepository = processedEventRepository;
        _historyRepository = historyRepository;
        _outboxRepository = outboxRepository;
        _paymentProvider = paymentProvider;
        _verificationService = verificationService;
        _logger = logger;
    }

    public async Task<WebhookResult> ProcessWebhookAsync(string rawBody, string signature, string correlationId)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = correlationId,
            ["Component"] = "WebhookService"
        });

        try
        {
            _logger.LogInformation("Processing webhook. BodyHash: {BodyHash}", ComputeSha256(rawBody).Substring(0, 16));

            // Step 1: Validate signature
            if (!_paymentProvider.ValidateSignature(rawBody, signature))
            {
                _logger.LogWarning("Webhook signature validation failed");
                return new WebhookResult { IsValid = false, IsSuccess = false, ErrorMessage = "Invalid signature" };
            }

            // Step 2: Check idempotency
            var eventHash = ComputeSha256(rawBody);
            if (await _processedEventRepository.IsProcessedByHashAsync(eventHash))
            {
                _logger.LogInformation("Webhook already processed");
                return new WebhookResult { IsValid = true, IsSuccess = true };
            }

            // Step 3: Parse webhook
            var webhookResult = _paymentProvider.ParseWebhookData(rawBody);
            _logger.LogInformation("Parsed webhook - OrderCode: {OrderCode}", webhookResult.OrderCode);

            // Step 4: Find payment
            var payment = await _paymentRepository.GetByOrderCodeAsync(webhookResult.OrderCode ?? "");
            if (payment == null)
            {
                _logger.LogWarning("Payment not found for OrderCode: {OrderCode}. Returning success to satisfy PayOS test ping.", webhookResult.OrderCode);
                return new WebhookResult { IsValid = true, IsSuccess = true };
            }

            // Step 5: Concurrency guard
            if (payment.Status != PaymentStatus.Pending)
            {
                _logger.LogInformation("Payment already processed");
                return new WebhookResult { IsValid = true, IsSuccess = true };
            }

            // Step 6: Atomic transition
            if (!await _paymentRepository.UpdateStatusConditionalAsync(payment.Id, PaymentStatus.Processing, payment.RowVersion))
            {
                _logger.LogInformation("Another webhook processing");
                return new WebhookResult { IsValid = true, IsSuccess = true };
            }

            // Step 7: Verify with API
            var webhookDataForVerification = new WebhookData
            {
                OrderCode = webhookResult.OrderCode ?? "",
                Amount = webhookResult.Amount,
                Status = webhookResult.IsValid && webhookResult.IsSuccess ? "PAID" : "FAILED",
                TransactionId = webhookResult.TransactionId ?? ""
            };

            var verificationResult = await _verificationService.VerifyWebhookDataAsync(payment, webhookDataForVerification);
            if (!verificationResult.IsValid)
            {
                _logger.LogCritical("Verification failed");
                await _paymentRepository.UpdateStatusAsync(payment.Id, PaymentStatus.Failed);
                return new WebhookResult { IsValid = false, IsSuccess = false, ErrorMessage = verificationResult.FailureReason };
            }

            // Step 8: Process in transaction
            var success = await ProcessPaymentTransactionAsync(payment, webhookDataForVerification, eventHash, correlationId);

            return new WebhookResult
            {
                IsValid = true,
                IsSuccess = success,
                OrderCode = payment.OrderCode,
                TransactionId = webhookResult.TransactionId,
                Amount = payment.Amount
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook exception");
            return new WebhookResult { IsValid = false, IsSuccess = false, ErrorMessage = ex.Message };
        }
    }

    private async Task<bool> ProcessPaymentTransactionAsync(
        PaymentEntity payment,
        WebhookData webhookData,
        string eventHash,
        string correlationId)
    {
        var transaction = await _paymentRepository.BeginTransactionAsync();

        try
        {
            payment.Status = webhookData.Status == "PAID" ? PaymentStatus.Succeeded : PaymentStatus.Failed;
            payment.TransactionId = webhookData.TransactionId;
            payment.UpdatedAt = DateTime.UtcNow;
            await _paymentRepository.UpdateAsync(payment);

            await _historyRepository.CreateAsync(new PaymentHistory
            {
                Id = Guid.NewGuid(),
                PaymentId = payment.Id,
                OldStatus = PaymentStatus.Processing.ToString(),
                NewStatus = payment.Status.ToString(),
                Action = $"Webhook processed: {webhookData.Status}",
                CorrelationId = correlationId,
                CreatedAt = DateTime.UtcNow
            });

            await _processedEventRepository.CreateAsync(new ProcessedEvent
            {
                Id = Guid.NewGuid(),
                EventHash = eventHash,
                OrderCode = webhookData.OrderCode,
                ProcessedAt = DateTime.UtcNow,
                CorrelationId = correlationId,
                EventId = payment.Id.ToString(),
                EventType = "webhook"
            });

            if (payment.Status == PaymentStatus.Succeeded)
            {
                await _outboxRepository.CreateAsync(new OutboxEvent
                {
                    EventId = Guid.NewGuid().ToString(),
                    EventType = "PaymentSucceeded",
                    Payload = JsonSerializer.Serialize(new
                    {
                        PaymentId = payment.Id,
                        SubscriptionId = payment.SubscriptionId,
                        UserId = payment.UserId,
                        Amount = payment.Amount,
                        TransactionId = payment.TransactionId
                    }),
                    Status = OutboxStatus.Pending,
                    CreatedAt = DateTime.UtcNow
                });
            }

            await ((dynamic)transaction).CommitAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Transaction failed");
            await ((dynamic)transaction).RollbackAsync();
            return false;
        }
    }

    private string ComputeSha256(string input)
    {
        using var sha256 = SHA256.Create();
        return Convert.ToHexString(sha256.ComputeHash(Encoding.UTF8.GetBytes(input)));
    }
}

public class WebhookData
{
    public string OrderCode { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string TransactionId { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

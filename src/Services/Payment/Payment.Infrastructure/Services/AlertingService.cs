using Microsoft.Extensions.Logging;
using Payment.Domain.Enums;
using System.Net.Http.Json;

namespace Payment.Infrastructure.Services;

/// <summary>
/// Alerting service for critical payment events.
/// Supports webhook notifications for monitoring systems.
/// </summary>
public interface IAlertingService
{
    Task SendAlertAsync(AlertType type, string message, Dictionary<string, object>? metadata = null);
    Task SendPaymentFailureAlertAsync(Guid paymentId, string reason, decimal amount);
    Task SendHighFailureRateAlertAsync(double failureRate, int totalPayments);
    Task SendWebhookProcessingFailureAlertAsync(string orderCode, string error);
    Task SendDLQThresholdAlertAsync(int queueSize);
}

public enum AlertType
{
    Info,
    Warning,
    Error,
    Critical
}

public class AlertingService : IAlertingService
{
    private readonly ILogger<AlertingService> _logger;
    private readonly HttpClient? _httpClient;
    private readonly string? _webhookUrl;
    private readonly bool _isConfigured;

    public AlertingService(ILogger<AlertingService> logger, HttpClient? httpClient = null)
    {
        _logger = logger;
        _httpClient = httpClient;
        
        _webhookUrl = Environment.GetEnvironmentVariable("ALERT_WEBHOOK_URL");
        _isConfigured = !string.IsNullOrEmpty(_webhookUrl);
    }

    public async Task SendAlertAsync(AlertType type, string message, Dictionary<string, object>? metadata = null)
    {
        var alert = new
        {
            Type = type.ToString(),
            Message = message,
            Service = "Payment",
            Timestamp = DateTime.UtcNow,
            Metadata = metadata ?? new Dictionary<string, object>()
        };

        // Always log the alert
        switch (type)
        {
            case AlertType.Critical:
            case AlertType.Error:
                _logger.LogError("ALERT [{Type}]: {Message} | Metadata: {@Metadata}", 
                    type, message, metadata);
                break;
            case AlertType.Warning:
                _logger.LogWarning("ALERT [{Type}]: {Message} | Metadata: {@Metadata}", 
                    type, message, metadata);
                break;
            default:
                _logger.LogInformation("ALERT [{Type}]: {Message} | Metadata: {@Metadata}", 
                    type, message, metadata);
                break;
        }

        // Send to webhook if configured
        if (_isConfigured && _httpClient != null)
        {
            try
            {
                await _httpClient.PostAsJsonAsync(_webhookUrl, alert);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send alert to webhook");
            }
        }
    }

    public async Task SendPaymentFailureAlertAsync(Guid paymentId, string reason, decimal amount)
    {
        await SendAlertAsync(AlertType.Warning, $"Payment failed: {reason}", new Dictionary<string, object>
        {
            { "PaymentId", paymentId },
            { "Reason", reason },
            { "Amount", amount }
        });
    }

    public async Task SendHighFailureRateAlertAsync(double failureRate, int totalPayments)
    {
        var type = failureRate > 20 ? AlertType.Critical : AlertType.Warning;
        
        await SendAlertAsync(type, $"High payment failure rate detected: {failureRate:F1}%", new Dictionary<string, object>
        {
            { "FailureRate", failureRate },
            { "TotalPayments", totalPayments },
            { "Threshold", 10 }
        });
    }

    public async Task SendWebhookProcessingFailureAlertAsync(string orderCode, string error)
    {
        await SendAlertAsync(AlertType.Error, $"Webhook processing failed for order {orderCode}", new Dictionary<string, object>
        {
            { "OrderCode", orderCode },
            { "Error", error }
        });
    }

    public async Task SendDLQThresholdAlertAsync(int queueSize)
    {
        var type = queueSize > 100 ? AlertType.Critical : AlertType.Warning;
        
        await SendAlertAsync(type, $"Dead Letter Queue size exceeds threshold: {queueSize}", new Dictionary<string, object>
        {
            { "QueueSize", queueSize },
            { "Threshold", 50 }
        });
    }
}

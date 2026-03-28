using Microsoft.Extensions.Logging;
using Payment.Domain.Enums;
using Payment.Domain.Interfaces;
using System.Collections.Concurrent;

namespace Payment.Infrastructure.Services;

/// <summary>
/// Payment metrics service for monitoring and alerting.
/// Tracks payment counts, success rates, latencies.
/// </summary>
public interface IPaymentMetricsService
{
    void IncrementPaymentCreated(PaymentProvider provider);
    void IncrementPaymentSucceeded(PaymentProvider provider);
    void IncrementPaymentFailed(PaymentProvider provider, string reason);
    void IncrementWebhookReceived(PaymentProvider provider);
    void IncrementWebhookProcessed(PaymentProvider provider, bool success);
    void RecordPaymentLatency(PaymentProvider provider, TimeSpan duration);
    void RecordWebhookLatency(TimeSpan duration);
    PaymentMetrics GetMetrics();
    void ResetMetrics();
}

public class PaymentMetrics
{
    public int TotalPaymentsCreated { get; set; }
    public int TotalPaymentsSucceeded { get; set; }
    public int TotalPaymentsFailed { get; set; }
    public int TotalWebhooksReceived { get; set; }
    public int TotalWebhooksProcessed { get; set; }
    public int TotalWebhooksFailed { get; set; }
    public double AveragePaymentLatencyMs { get; set; }
    public double AverageWebhookLatencyMs { get; set; }
    public double SuccessRate => TotalPaymentsCreated > 0 
        ? (double)TotalPaymentsSucceeded / TotalPaymentsCreated * 100 
        : 0;
    public Dictionary<string, int> FailureReasons { get; set; } = new();
    public Dictionary<string, ProviderMetrics> ByProvider { get; set; } = new();
    public DateTime LastResetAt { get; set; }
}

public class ProviderMetrics
{
    public int Created { get; set; }
    public int Succeeded { get; set; }
    public int Failed { get; set; }
    public double AverageLatencyMs { get; set; }
}

public class PaymentMetricsService : IPaymentMetricsService
{
    private readonly ILogger<PaymentMetricsService> _logger;
    private readonly ConcurrentDictionary<PaymentProvider, ProviderMetrics> _providerMetrics = new();
    private readonly ConcurrentDictionary<string, int> _failureReasons = new();
    private readonly ConcurrentBag<double> _paymentLatencies = new();
    private readonly ConcurrentBag<double> _webhookLatencies = new();
    
    private int _totalPaymentsCreated;
    private int _totalPaymentsSucceeded;
    private int _totalPaymentsFailed;
    private int _totalWebhooksReceived;
    private int _totalWebhooksProcessed;
    private int _totalWebhooksFailed;
    private DateTime _lastResetAt = DateTime.UtcNow;

    public PaymentMetricsService(ILogger<PaymentMetricsService> logger)
    {
        _logger = logger;
    }

    public void IncrementPaymentCreated(PaymentProvider provider)
    {
        Interlocked.Increment(ref _totalPaymentsCreated);
        GetOrCreateProviderMetrics(provider).Created++;
        _logger.LogDebug("Payment created metric incremented for {Provider}", provider);
    }

    public void IncrementPaymentSucceeded(PaymentProvider provider)
    {
        Interlocked.Increment(ref _totalPaymentsSucceeded);
        GetOrCreateProviderMetrics(provider).Succeeded++;
        _logger.LogDebug("Payment succeeded metric incremented for {Provider}", provider);
    }

    public void IncrementPaymentFailed(PaymentProvider provider, string reason)
    {
        Interlocked.Increment(ref _totalPaymentsFailed);
        GetOrCreateProviderMetrics(provider).Failed++;
        _failureReasons.AddOrUpdate(reason, 1, (_, count) => count + 1);
        _logger.LogDebug("Payment failed metric incremented for {Provider}: {Reason}", provider, reason);
    }

    public void IncrementWebhookReceived(PaymentProvider provider)
    {
        Interlocked.Increment(ref _totalWebhooksReceived);
        _logger.LogDebug("Webhook received metric incremented for {Provider}", provider);
    }

    public void IncrementWebhookProcessed(PaymentProvider provider, bool success)
    {
        if (success)
        {
            Interlocked.Increment(ref _totalWebhooksProcessed);
        }
        else
        {
            Interlocked.Increment(ref _totalWebhooksFailed);
        }
    }

    public void RecordPaymentLatency(PaymentProvider provider, TimeSpan duration)
    {
        var ms = duration.TotalMilliseconds;
        _paymentLatencies.Add(ms);
        
        var metrics = GetOrCreateProviderMetrics(provider);
        // Simple running average approximation
        metrics.AverageLatencyMs = (metrics.AverageLatencyMs + ms) / 2;
    }

    public void RecordWebhookLatency(TimeSpan duration)
    {
        _webhookLatencies.Add(duration.TotalMilliseconds);
    }

    public PaymentMetrics GetMetrics()
    {
        return new PaymentMetrics
        {
            TotalPaymentsCreated = _totalPaymentsCreated,
            TotalPaymentsSucceeded = _totalPaymentsSucceeded,
            TotalPaymentsFailed = _totalPaymentsFailed,
            TotalWebhooksReceived = _totalWebhooksReceived,
            TotalWebhooksProcessed = _totalWebhooksProcessed,
            TotalWebhooksFailed = _totalWebhooksFailed,
            AveragePaymentLatencyMs = _paymentLatencies.Any() ? _paymentLatencies.Average() : 0,
            AverageWebhookLatencyMs = _webhookLatencies.Any() ? _webhookLatencies.Average() : 0,
            FailureReasons = new Dictionary<string, int>(_failureReasons),
            ByProvider = _providerMetrics.ToDictionary(
                kvp => kvp.Key.ToString(),
                kvp => kvp.Value),
            LastResetAt = _lastResetAt
        };
    }

    public void ResetMetrics()
    {
        _totalPaymentsCreated = 0;
        _totalPaymentsSucceeded = 0;
        _totalPaymentsFailed = 0;
        _totalWebhooksReceived = 0;
        _totalWebhooksProcessed = 0;
        _totalWebhooksFailed = 0;
        _providerMetrics.Clear();
        _failureReasons.Clear();
        _paymentLatencies.Clear();
        _webhookLatencies.Clear();
        _lastResetAt = DateTime.UtcNow;
        
        _logger.LogInformation("Payment metrics reset at {Time}", _lastResetAt);
    }

    private ProviderMetrics GetOrCreateProviderMetrics(PaymentProvider provider)
    {
        return _providerMetrics.GetOrAdd(provider, _ => new ProviderMetrics());
    }
}

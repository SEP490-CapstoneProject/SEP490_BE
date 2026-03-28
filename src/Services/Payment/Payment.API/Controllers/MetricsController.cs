using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payment.Infrastructure.Services;

namespace Payment.API.Controllers;

/// <summary>
/// Metrics endpoint for Prometheus scraping and monitoring.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class MetricsController : ControllerBase
{
    private readonly IPaymentMetricsService _metricsService;

    public MetricsController(IPaymentMetricsService metricsService)
    {
        _metricsService = metricsService;
    }

    /// <summary>
    /// Get current payment metrics (JSON format).
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public IActionResult GetMetrics()
    {
        var metrics = _metricsService.GetMetrics();
        return Ok(metrics);
    }

    /// <summary>
    /// Get metrics in Prometheus format.
    /// </summary>
    [HttpGet("prometheus")]
    [AllowAnonymous]
    [Produces("text/plain")]
    public IActionResult GetPrometheusMetrics()
    {
        var metrics = _metricsService.GetMetrics();
        
        var prometheusOutput = $@"# HELP payment_total_created Total payments created
# TYPE payment_total_created counter
payment_total_created {metrics.TotalPaymentsCreated}

# HELP payment_total_succeeded Total payments succeeded
# TYPE payment_total_succeeded counter
payment_total_succeeded {metrics.TotalPaymentsSucceeded}

# HELP payment_total_failed Total payments failed
# TYPE payment_total_failed counter
payment_total_failed {metrics.TotalPaymentsFailed}

# HELP payment_success_rate Payment success rate percentage
# TYPE payment_success_rate gauge
payment_success_rate {metrics.SuccessRate:F2}

# HELP webhook_total_received Total webhooks received
# TYPE webhook_total_received counter
webhook_total_received {metrics.TotalWebhooksReceived}

# HELP webhook_total_processed Total webhooks processed successfully
# TYPE webhook_total_processed counter
webhook_total_processed {metrics.TotalWebhooksProcessed}

# HELP webhook_total_failed Total webhooks failed
# TYPE webhook_total_failed counter
webhook_total_failed {metrics.TotalWebhooksFailed}

# HELP payment_latency_avg_ms Average payment creation latency in milliseconds
# TYPE payment_latency_avg_ms gauge
payment_latency_avg_ms {metrics.AveragePaymentLatencyMs:F2}

# HELP webhook_latency_avg_ms Average webhook processing latency in milliseconds
# TYPE webhook_latency_avg_ms gauge
webhook_latency_avg_ms {metrics.AverageWebhookLatencyMs:F2}
";

        // Add per-provider metrics
        foreach (var provider in metrics.ByProvider)
        {
            prometheusOutput += $@"
# HELP payment_by_provider_created Payments created by provider
# TYPE payment_by_provider_created counter
payment_by_provider_created{{provider=""{provider.Key}""}} {provider.Value.Created}

# HELP payment_by_provider_succeeded Payments succeeded by provider
# TYPE payment_by_provider_succeeded counter
payment_by_provider_succeeded{{provider=""{provider.Key}""}} {provider.Value.Succeeded}

# HELP payment_by_provider_failed Payments failed by provider  
# TYPE payment_by_provider_failed counter
payment_by_provider_failed{{provider=""{provider.Key}""}} {provider.Value.Failed}
";
        }

        return Content(prometheusOutput, "text/plain");
    }

    /// <summary>
    /// Reset metrics (admin only).
    /// </summary>
    [HttpPost("reset")]
    [Authorize(Roles = "Admin")]
    public IActionResult ResetMetrics()
    {
        _metricsService.ResetMetrics();
        return Ok(new { message = "Metrics reset successfully" });
    }
}

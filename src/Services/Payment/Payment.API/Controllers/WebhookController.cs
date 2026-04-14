using Microsoft.AspNetCore.Mvc;
using Payment.Application.Interfaces;

namespace Payment.API.Controllers;

[ApiController]
[Route("api/payments/webhook")]
public class WebhookController : ControllerBase
{
    private readonly IWebhookService _webhookService;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(IWebhookService webhookService, ILogger<WebhookController> logger)
    {
        _webhookService = webhookService;
        _logger = logger;
    }

    /// <summary>
    /// PayOS webhook verification ping (GET) - returns 200 OK so PayOS accepts the URL
    /// </summary>
    [HttpGet("payos")]
    public IActionResult PayOSWebhookPing()
    {
        return Ok(new { code = "00", message = "Webhook endpoint ready" });
    }

    /// <summary>
    /// PayOS webhook endpoint (POST JSON)
    /// CRITICAL: Must read raw body BEFORE parsing for signature validation
    /// </summary>
    [HttpPost("payos")]
    public async Task<IActionResult> PayOSWebhook()
    {
        var correlationId = Guid.NewGuid().ToString();
        
        try
        {
            // Step 1: Read raw body (required for signature validation)
            Request.EnableBuffering();
            using var reader = new StreamReader(Request.Body, leaveOpen: true);
            var rawBody = await reader.ReadToEndAsync();
            Request.Body.Position = 0;

            // Step 2: Get signature from header
            var signature = Request.Headers["x-payos-signature"].FirstOrDefault();
            if (string.IsNullOrEmpty(signature))
            {
                _logger.LogWarning("PayOS webhook missing signature header. CorrelationId: {CorrelationId}", 
                    correlationId);
                // Still return 200 so PayOS accepts the webhook URL
                return Ok(new { code = "01", message = "Missing signature" });
            }

            _logger.LogInformation("PayOS webhook received. CorrelationId: {CorrelationId}, BodyLength: {Length}", 
                correlationId, rawBody.Length);

            // Step 3: Process webhook (validates, verifies, processes in transaction)
            var result = await _webhookService.ProcessWebhookAsync(rawBody, signature, correlationId);

            if (result.IsSuccess)
            {
                return Ok(new { code = "00", message = "Success" });
            }

            _logger.LogWarning("PayOS webhook failed. Reason: {Reason}, CorrelationId: {CorrelationId}",
                result.ErrorMessage, correlationId);

            // Return 200 with non-zero code so PayOS doesn't retry endlessly
            return Ok(new { code = "99", message = result.ErrorMessage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PayOS webhook exception. CorrelationId: {CorrelationId}", correlationId);
            return Ok(new { code = "99", message = "Internal error" });
        }
    }
}

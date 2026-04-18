using Microsoft.AspNetCore.Mvc;
using Payment.Application.Interfaces;
using System.Text.Json;

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

            // Step 2: Get signature (header first, then fallback from body.signature)
            var signature = Request.Headers["x-payos-signature"].FirstOrDefault();
            if (string.IsNullOrWhiteSpace(signature))
            {
                signature = TryExtractSignatureFromBody(rawBody);
            }

            if (string.IsNullOrEmpty(signature))
            {
                _logger.LogWarning("PayOS webhook missing signature in both header and body. CorrelationId: {CorrelationId}",
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

    private static string? TryExtractSignatureFromBody(string rawBody)
    {
        if (string.IsNullOrWhiteSpace(rawBody))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(rawBody);
            if (document.RootElement.TryGetProperty("signature", out var signatureElement) &&
                signatureElement.ValueKind == JsonValueKind.String)
            {
                return signatureElement.GetString();
            }
        }
        catch (JsonException)
        {
            return null;
        }

        return null;
    }
}

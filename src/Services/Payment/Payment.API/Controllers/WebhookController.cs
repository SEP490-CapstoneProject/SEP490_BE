using Microsoft.AspNetCore.Mvc;
using Payment.Application.Interfaces;

namespace Payment.API.Controllers;

[ApiController]
[Route("api/payments/webhook")]
public class WebhookController : ControllerBase
{
    private readonly IWebhookHandler _webhookHandler;
    private readonly ILogger<WebhookController> _logger;

    public WebhookController(IWebhookHandler webhookHandler, ILogger<WebhookController> logger)
    {
        _webhookHandler = webhookHandler;
        _logger = logger;
    }

    [HttpGet("vnpay")]
    [HttpPost("vnpay")]
    public async Task<IActionResult> VnPayWebhook()
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("VNPay webhook received. CorrelationId: {CorrelationId}", correlationId);

        var queryParams = Request.Query.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.ToString()
        );

        var success = await _webhookHandler.HandleWebhookAsync("vnpay", queryParams, correlationId);

        if (success)
        {
            return Ok(new { RspCode = "00", Message = "Success" });
        }

        return BadRequest(new { RspCode = "99", Message = "Failed" });
    }

    [HttpGet("momo")]
    [HttpPost("momo")]
    public async Task<IActionResult> MoMoWebhook()
    {
        var correlationId = Guid.NewGuid().ToString();
        _logger.LogInformation("MoMo webhook received. CorrelationId: {CorrelationId}", correlationId);

        var queryParams = Request.Query.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.ToString()
        );

        // For POST, also check body
        if (Request.Method == "POST" && Request.HasFormContentType)
        {
            foreach (var kv in Request.Form)
            {
                queryParams[kv.Key] = kv.Value.ToString();
            }
        }

        var success = await _webhookHandler.HandleWebhookAsync("momo", queryParams, correlationId);

        if (success)
        {
            return Ok(new { resultCode = 0, message = "Success" });
        }

        return BadRequest(new { resultCode = 99, message = "Failed" });
    }
}

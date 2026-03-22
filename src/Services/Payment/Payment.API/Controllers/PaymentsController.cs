using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;
using System.Security.Claims;

namespace Payment.API.Controllers;

[ApiController]
[Route("api/payments")]
[Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(IPaymentService paymentService, ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    [HttpPost("create")]
    public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest request)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new { message = "Invalid user token" });

        try
        {
            var response = await _paymentService.CreatePaymentAsync(userId.Value, request);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payment for user {UserId}", userId);
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{paymentId}")]
    public async Task<IActionResult> GetPayment(Guid paymentId)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new { message = "Invalid user token" });

        var payment = await _paymentService.GetPaymentByIdAsync(paymentId);
        if (payment == null)
            return NotFound(new { message = "Payment not found" });

        // Users can only view their own payments
        if (payment.UserId != userId.Value && !User.IsInRole("Admin"))
            return Forbid();

        return Ok(payment);
    }

    [HttpGet("my-history")]
    public async Task<IActionResult> GetMyPaymentHistory([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(new { message = "Invalid user token" });

        var payments = await _paymentService.GetUserPaymentsAsync(userId.Value, page, pageSize);
        return Ok(payments);
    }

    private int? GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier) ?? User.FindFirst("sub") ?? User.FindFirst("userId");
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            return null;

        return userId;
    }
}

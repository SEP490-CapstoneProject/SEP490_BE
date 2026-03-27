using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Payment.Application.DTOs;
using Payment.Application.Interfaces;

namespace Payment.API.Controllers;

[ApiController]
[Route("api/admin/payments")]
[Authorize(Roles = "Admin")]
public class AdminPaymentsController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<AdminPaymentsController> _logger;

    public AdminPaymentsController(IPaymentService paymentService, ILogger<AdminPaymentsController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllPayments([FromQuery] AdminPaymentFilterRequest filter)
    {
        try
        {
            var result = await _paymentService.GetAllPaymentsAsync(filter);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payments with filter");
            return StatusCode(500, new { message = "Internal server error" });
        }
    }

    [HttpGet("{paymentId}")]
    public async Task<IActionResult> GetPayment(Guid paymentId)
    {
        var payment = await _paymentService.GetPaymentByIdAsync(paymentId);
        if (payment == null)
            return NotFound(new { message = "Payment not found" });

        return Ok(payment);
    }
}

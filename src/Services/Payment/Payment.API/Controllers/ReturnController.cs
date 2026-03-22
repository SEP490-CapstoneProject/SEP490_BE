using Microsoft.AspNetCore.Mvc;
using Payment.Application.Interfaces;

namespace Payment.API.Controllers;

[ApiController]
[Route("api/payments/return")]
public class ReturnController : ControllerBase
{
    private readonly IPaymentService _paymentService;
    private readonly ILogger<ReturnController> _logger;

    public ReturnController(IPaymentService paymentService, ILogger<ReturnController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    [HttpGet("vnpay")]
    public async Task<IActionResult> VnPayReturn([FromQuery] string vnp_TxnRef)
    {
        _logger.LogInformation("VNPay return URL called for OrderCode: {OrderCode}", vnp_TxnRef);

        if (string.IsNullOrEmpty(vnp_TxnRef))
            return Redirect("/payment-error");

        var payment = await _paymentService.GetPaymentByOrderCodeAsync(vnp_TxnRef);
        if (payment == null)
            return Redirect("/payment-error");

        // Redirect to frontend based on status
        return payment.Status switch
        {
            "Success" => Redirect($"/payment-success?paymentId={payment.Id}"),
            "Failed" => Redirect($"/payment-failed?paymentId={payment.Id}"),
            _ => Redirect($"/payment-pending?paymentId={payment.Id}")
        };
    }

    [HttpGet("momo")]
    public async Task<IActionResult> MoMoReturn([FromQuery] string orderId)
    {
        _logger.LogInformation("MoMo return URL called for OrderCode: {OrderCode}", orderId);

        if (string.IsNullOrEmpty(orderId))
            return Redirect("/payment-error");

        var payment = await _paymentService.GetPaymentByOrderCodeAsync(orderId);
        if (payment == null)
            return Redirect("/payment-error");

        return payment.Status switch
        {
            "Success" => Redirect($"/payment-success?paymentId={payment.Id}"),
            "Failed" => Redirect($"/payment-failed?paymentId={payment.Id}"),
            _ => Redirect($"/payment-pending?paymentId={payment.Id}")
        };
    }
}

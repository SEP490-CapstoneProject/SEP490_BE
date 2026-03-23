namespace Subscription.Application.DTOs.Admin;

public class RefundRequest
{
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
}

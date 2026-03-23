namespace Subscription.Application.DTOs.Admin;

public class AdminCancelRequest
{
    public string Reason { get; set; } = string.Empty;
    public bool IssueRefund { get; set; }
}

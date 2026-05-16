namespace Subscription.Application.DTOs.Admin;

public class UpdatePlanRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public int? BillingCycle { get; set; }
    /// <summary>
    /// Role được phép mua plan. Null = không thay đổi. Dùng empty string "" để xóa restriction (cho phép tất cả role).
    /// Ví dụ: "JobSeeker", "Employer", "" (xóa restriction)
    /// </summary>
    public string? AllowedRole { get; set; }
    public bool ClearAllowedRole { get; set; } = false;
}

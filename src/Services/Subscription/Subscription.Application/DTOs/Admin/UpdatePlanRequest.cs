namespace Subscription.Application.DTOs.Admin;

public class UpdatePlanRequest
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public decimal? Price { get; set; }
    public int? BillingCycle { get; set; }
}

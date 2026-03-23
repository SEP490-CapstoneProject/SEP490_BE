namespace Subscription.Application.DTOs.Admin;

public class CreatePlanRequest
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int BillingCycle { get; set; }  // 1=Monthly, 2=Yearly
}

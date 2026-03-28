namespace Subscription.Application.DTOs.Admin;

public class RevenueAnalyticsDto
{
    public decimal TotalRevenue { get; set; }
    public Dictionary<string, decimal> RevenueByPlan { get; set; } = new();
    public List<DailyRevenueDto> DailyRevenue { get; set; } = new();
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public class DailyRevenueDto
{
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
}

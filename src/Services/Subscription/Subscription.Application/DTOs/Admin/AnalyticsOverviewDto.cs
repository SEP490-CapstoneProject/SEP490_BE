namespace Subscription.Application.DTOs.Admin;

public class AnalyticsOverviewDto
{
    public int TotalUsers { get; set; }
    public int ActiveSubscriptions { get; set; }
    public decimal TotalRevenue { get; set; }
    public double ChurnRate { get; set; }
    public decimal MRR { get; set; }  // Monthly Recurring Revenue
    public decimal ARR { get; set; }  // Annual Recurring Revenue
    public DateTime GeneratedAt { get; set; }
}

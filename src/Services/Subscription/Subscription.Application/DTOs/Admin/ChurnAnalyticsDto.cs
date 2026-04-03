namespace Subscription.Application.DTOs.Admin;

public class ChurnAnalyticsDto
{
    public double ChurnRate { get; set; }  // Percentage
    public int ChurnedUsers { get; set; }
    public int TotalUsers { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

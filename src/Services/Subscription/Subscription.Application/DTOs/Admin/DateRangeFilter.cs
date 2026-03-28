namespace Subscription.Application.DTOs.Admin;

public class DateRangeFilter
{
    public DateTime StartDate { get; set; } = DateTime.UtcNow.AddMonths(-1);
    public DateTime EndDate { get; set; } = DateTime.UtcNow;
}

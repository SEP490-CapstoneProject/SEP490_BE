using Microsoft.EntityFrameworkCore;
using Subscription.Application.DTOs.Admin;
using Subscription.Domain.Enums;
using Subscription.Infrastructure.Data;

namespace Subscription.Infrastructure.Services;

public class AnalyticsService
{
    private readonly SubscriptionDbContext _context;

    public AnalyticsService(SubscriptionDbContext context)
    {
        _context = context;
    }

    public async Task<AnalyticsOverviewDto> GetAnalyticsOverviewAsync(DateRangeFilter filter)
    {
        var now = DateTime.UtcNow;
        
        // Total users who ever subscribed
        var totalUsers = await _context.Subscriptions
            .Select(s => s.UserId)
            .Distinct()
            .CountAsync();

        // Active subscriptions
        var activeSubscriptions = await _context.Subscriptions
            .CountAsync(s => s.Status == SubscriptionStatus.Active);

        // Total revenue (all time paid subscriptions)
        var totalRevenue = await _context.Subscriptions
            .Where(s => s.PaymentStatus == PaymentStatus.Paid)
            .Join(_context.Plans, s => s.PlanId, p => p.Id, (s, p) => p.Price)
            .SumAsync();

        // Calculate churn rate (last 30 days)
        var thirtyDaysAgo = now.AddDays(-30);
        var cancelledLast30Days = await _context.Subscriptions
            .CountAsync(s => s.Status == SubscriptionStatus.Cancelled && s.UpdatedAt >= thirtyDaysAgo);
        
        var activeStartOfPeriod = await _context.Subscriptions
            .CountAsync(s => s.Status == SubscriptionStatus.Active || 
                           (s.Status == SubscriptionStatus.Cancelled && s.UpdatedAt >= thirtyDaysAgo));
        
        var churnRate = activeStartOfPeriod > 0 ? (double)cancelledLast30Days / activeStartOfPeriod * 100 : 0;

        // Calculate MRR (Monthly Recurring Revenue) - active monthly subscriptions
        var mrr = await _context.Subscriptions
            .Where(s => s.Status == SubscriptionStatus.Active)
            .Join(_context.Plans.Where(p => p.BillingCycle == BillingCycle.Monthly), 
                  s => s.PlanId, p => p.Id, (s, p) => p.Price)
            .SumAsync();

        // Calculate ARR (Annual Recurring Revenue) - active yearly subscriptions * 12 + monthly * 12
        var yearlyRevenue = await _context.Subscriptions
            .Where(s => s.Status == SubscriptionStatus.Active)
            .Join(_context.Plans.Where(p => p.BillingCycle == BillingCycle.Yearly), 
                  s => s.PlanId, p => p.Id, (s, p) => p.Price)
            .SumAsync();
        
        var arr = (mrr * 12) + yearlyRevenue;

        return new AnalyticsOverviewDto
        {
            TotalUsers = totalUsers,
            ActiveSubscriptions = activeSubscriptions,
            TotalRevenue = totalRevenue,
            ChurnRate = Math.Round(churnRate, 2),
            MRR = mrr,
            ARR = arr,
            GeneratedAt = now
        };
    }

    public async Task<IEnumerable<SubscriptionCountByPlanDto>> GetSubscriptionsByPlanAsync()
    {
        var result = await _context.Plans
            .Select(p => new SubscriptionCountByPlanDto
            {
                PlanId = p.Id,
                PlanName = p.Name,
                ActiveCount = _context.Subscriptions.Count(s => s.PlanId == p.Id && s.Status == SubscriptionStatus.Active),
                ExpiredCount = _context.Subscriptions.Count(s => s.PlanId == p.Id && s.Status == SubscriptionStatus.Expired),
                CancelledCount = _context.Subscriptions.Count(s => s.PlanId == p.Id && s.Status == SubscriptionStatus.Cancelled),
                TotalCount = _context.Subscriptions.Count(s => s.PlanId == p.Id)
            })
            .ToListAsync();

        return result;
    }

    public async Task<ChurnAnalyticsDto> GetChurnRateAsync(DateRangeFilter filter)
    {
        // Users who cancelled in the period
        var churnedUsers = await _context.Subscriptions
            .Where(s => s.Status == SubscriptionStatus.Cancelled &&
                       s.UpdatedAt >= filter.StartDate &&
                       s.UpdatedAt <= filter.EndDate)
            .Select(s => s.UserId)
            .Distinct()
            .CountAsync();

        // Total users who were active at the start of period
        var totalUsers = await _context.Subscriptions
            .Where(s => (s.Status == SubscriptionStatus.Active || 
                        (s.Status == SubscriptionStatus.Cancelled && s.UpdatedAt >= filter.StartDate)) &&
                       s.StartDate < filter.StartDate)
            .Select(s => s.UserId)
            .Distinct()
            .CountAsync();

        var churnRate = totalUsers > 0 ? (double)churnedUsers / totalUsers * 100 : 0;

        return new ChurnAnalyticsDto
        {
            ChurnRate = Math.Round(churnRate, 2),
            ChurnedUsers = churnedUsers,
            TotalUsers = totalUsers,
            StartDate = filter.StartDate,
            EndDate = filter.EndDate
        };
    }

    public async Task<RevenueAnalyticsDto> GetRevenueAnalyticsAsync(DateRangeFilter filter)
    {
        // Total revenue in date range
        var totalRevenue = await _context.Subscriptions
            .Where(s => s.PaymentStatus == PaymentStatus.Paid &&
                       s.StartDate >= filter.StartDate &&
                       s.StartDate <= filter.EndDate)
            .Join(_context.Plans, s => s.PlanId, p => p.Id, (s, p) => p.Price)
            .SumAsync();

        // Revenue by plan
        var revenueByPlan = await _context.Subscriptions
            .Where(s => s.PaymentStatus == PaymentStatus.Paid &&
                       s.StartDate >= filter.StartDate &&
                       s.StartDate <= filter.EndDate)
            .Join(_context.Plans, s => s.PlanId, p => p.Id, (s, p) => new { s, p })
            .GroupBy(x => x.p.Name)
            .Select(g => new { PlanName = g.Key, Revenue = g.Sum(x => x.p.Price) })
            .ToDictionaryAsync(x => x.PlanName, x => x.Revenue);

        // Daily revenue
        var dailyRevenue = await _context.Subscriptions
            .Where(s => s.PaymentStatus == PaymentStatus.Paid &&
                       s.StartDate >= filter.StartDate &&
                       s.StartDate <= filter.EndDate)
            .Join(_context.Plans, s => s.PlanId, p => p.Id, (s, p) => new { s.StartDate, p.Price })
            .GroupBy(x => x.StartDate.Date)
            .Select(g => new DailyRevenueDto
            {
                Date = g.Key,
                Revenue = g.Sum(x => x.Price)
            })
            .OrderBy(x => x.Date)
            .ToListAsync();

        return new RevenueAnalyticsDto
        {
            TotalRevenue = totalRevenue,
            RevenueByPlan = revenueByPlan,
            DailyRevenue = dailyRevenue,
            StartDate = filter.StartDate,
            EndDate = filter.EndDate
        };
    }
}

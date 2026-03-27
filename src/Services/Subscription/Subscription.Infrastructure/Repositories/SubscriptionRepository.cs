using Microsoft.EntityFrameworkCore;
using Subscription.Application.Interfaces;
using Subscription.Domain.Entities;
using Subscription.Domain.Enums;
using Subscription.Infrastructure.Data;

namespace Subscription.Infrastructure.Repositories;

public class SubscriptionRepository : ISubscriptionRepository
{
    private readonly SubscriptionDbContext _context;

    public SubscriptionRepository(SubscriptionDbContext context)
    {
        _context = context;
    }

    public async Task<UserSubscription?> GetByIdAsync(int id)
    {
        return await _context.Subscriptions
            .Include(s => s.Plan)
            .ThenInclude(p => p.Features)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<UserSubscription?> GetActiveByUserIdAsync(int userId)
    {
        return await _context.Subscriptions
            .Include(s => s.Plan)
            .ThenInclude(p => p.Features)
            .Where(s => s.UserId == userId && s.Status == SubscriptionStatus.Active)
            .OrderByDescending(s => s.EndDate)
            .FirstOrDefaultAsync();
    }

    public async Task<IEnumerable<UserSubscription>> GetByUserIdAsync(int userId)
    {
        return await _context.Subscriptions
            .Include(s => s.Plan)
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<UserSubscription>> GetActiveSubscriptionsAsync()
    {
        return await _context.Subscriptions
            .Include(s => s.Plan)
            .ThenInclude(p => p.Features)
            .Where(s => s.Status == SubscriptionStatus.Active)
            .ToListAsync();
    }

    public async Task<IEnumerable<UserSubscription>> GetRecentlyActiveSubscriptionsAsync(int days)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days);
        return await _context.Subscriptions
            .Include(s => s.Plan)
            .ThenInclude(p => p.Features)
            .Where(s => s.Status == SubscriptionStatus.Active || s.UpdatedAt >= cutoffDate)
            .ToListAsync();
    }

    public async Task<UserSubscription> CreateAsync(UserSubscription subscription)
    {
        subscription.CreatedAt = DateTime.UtcNow;
        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();
        return subscription;
    }

    public async Task UpdateAsync(UserSubscription subscription)
    {
        subscription.UpdatedAt = DateTime.UtcNow;
        _context.Subscriptions.Update(subscription);
        await _context.SaveChangesAsync();
    }

    public async Task<bool> HasActiveSubscriptionAsync(int userId)
    {
        return await _context.Subscriptions
            .AnyAsync(s => s.UserId == userId && s.Status == SubscriptionStatus.Active);
    }
}

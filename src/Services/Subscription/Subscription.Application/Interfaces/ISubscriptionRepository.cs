using Subscription.Domain.Entities;

namespace Subscription.Application.Interfaces;

public interface ISubscriptionRepository
{
    Task<UserSubscription?> GetByIdAsync(int id);
    Task<UserSubscription?> GetActiveByUserIdAsync(int userId);
    Task<IEnumerable<UserSubscription>> GetByUserIdAsync(int userId);
    Task<IEnumerable<UserSubscription>> GetActiveSubscriptionsAsync();
    Task<IEnumerable<UserSubscription>> GetRecentlyActiveSubscriptionsAsync(int days);
    Task<UserSubscription> CreateAsync(UserSubscription subscription);
    Task UpdateAsync(UserSubscription subscription);
    Task<bool> HasActiveSubscriptionAsync(int userId);
}

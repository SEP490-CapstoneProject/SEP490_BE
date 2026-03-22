using Subscription.Application.DTOs;

namespace Subscription.Application.Interfaces;

public interface ISubscriptionService
{
    Task<IEnumerable<PlanDto>> GetAllPlansAsync();
    Task<PlanDto?> GetPlanByIdAsync(int planId);
    Task<SubscriptionDto> SubscribeAsync(int userId, SubscribeRequest request);
    Task<SubscriptionDto> UpgradeAsync(int userId, UpgradeRequest request);
    Task CancelSubscriptionAsync(int userId, CancelRequest request);
    Task<SubscriptionDto?> GetActiveSubscriptionAsync(int userId);
    Task<EntitlementsDto?> GetEntitlementsAsync(int userId);
    Task ActivateSubscriptionAsync(int subscriptionId, string eventId);
    Task ExpireSubscriptionAsync(int subscriptionId);
}

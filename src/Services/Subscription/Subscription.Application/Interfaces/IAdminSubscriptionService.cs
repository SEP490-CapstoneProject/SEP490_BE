using Subscription.Application.DTOs;
using Subscription.Application.DTOs.Admin;

namespace Subscription.Application.Interfaces;

public interface IAdminSubscriptionService
{
    // Plan Management
    Task<PlanDto> CreatePlanAsync(CreatePlanRequest request);
    Task<PlanDto> UpdatePlanAsync(int planId, UpdatePlanRequest request);
    Task DeletePlanAsync(int planId);
    Task<PlanDto> TogglePlanActiveAsync(int planId);
    
    // Plan Features
    Task<DTOs.Admin.PlanFeatureDto> AddPlanFeatureAsync(int planId, CreatePlanFeatureRequest request);
    Task<DTOs.Admin.PlanFeatureDto> UpdatePlanFeatureAsync(int planId, int featureId, UpdatePlanFeatureRequest request);
    Task DeletePlanFeatureAsync(int planId, int featureId);
    
    // Subscription Management
    Task<IEnumerable<AdminSubscriptionDto>> GetAllSubscriptionsAsync(SubscriptionFilter filter);
    Task<AdminSubscriptionDto?> GetSubscriptionByIdAsync(int subscriptionId);
    Task<IEnumerable<AdminSubscriptionDto>> GetUserSubscriptionHistoryAsync(int userId);
    Task CancelSubscriptionAsync(int subscriptionId, AdminCancelRequest request);
    Task ExtendSubscriptionAsync(int subscriptionId, ExtendRequest request);
    Task IssueRefundAsync(int subscriptionId, RefundRequest request);
    
    // Analytics
    Task<AnalyticsOverviewDto> GetAnalyticsOverviewAsync(DateRangeFilter filter);
    Task<RevenueAnalyticsDto> GetRevenueAnalyticsAsync(DateRangeFilter filter);
    Task<IEnumerable<SubscriptionCountByPlanDto>> GetSubscriptionsByPlanAsync();
    Task<ChurnAnalyticsDto> GetChurnRateAsync(DateRangeFilter filter);
}

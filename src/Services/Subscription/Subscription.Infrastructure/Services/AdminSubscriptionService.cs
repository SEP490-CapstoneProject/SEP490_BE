using Microsoft.EntityFrameworkCore;
using Subscription.Application.DTOs;
using Subscription.Application.DTOs.Admin;
using Subscription.Application.Interfaces;
using Subscription.Domain.Entities;
using Subscription.Infrastructure.Data;
using System.Text.Json;

namespace Subscription.Infrastructure.Services;

public class AdminSubscriptionService : IAdminSubscriptionService
{
    private readonly SubscriptionDbContext _context;
    private readonly IAuditLogService _auditLogService;
    private readonly ISubscriptionUserProfileClient _userProfileClient;

    public AdminSubscriptionService(
        SubscriptionDbContext context,
        IAuditLogService auditLogService,
        ISubscriptionUserProfileClient userProfileClient)
    {
        _context = context;
        _auditLogService = auditLogService;
        _userProfileClient = userProfileClient;
    }

    public async Task<PlanDto> CreatePlanAsync(CreatePlanRequest request)
    {
        var plan = new Plan
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            BillingCycle = (Domain.Enums.BillingCycle)request.BillingCycle,
            IsActive = true,
            AllowedRole = string.IsNullOrWhiteSpace(request.AllowedRole) ? null : request.AllowedRole.Trim(),
            CreatedAt = DateTime.UtcNow
        };

        _context.Plans.Add(plan);
        await _context.SaveChangesAsync();

        // Audit log
        await _auditLogService.LogActionAsync(0, "Create", "Plan", plan.Id, null, JsonSerializer.Serialize(plan));

        return MapToPlanDto(plan);
    }

    public async Task<PlanDto> UpdatePlanAsync(int planId, UpdatePlanRequest request)
    {
        var plan = await _context.Plans.FindAsync(planId);
        if (plan == null)
            throw new KeyNotFoundException($"Plan with ID {planId} not found");

        var oldValues = JsonSerializer.Serialize(plan);

        if (!string.IsNullOrEmpty(request.Name))
            plan.Name = request.Name;
        
        if (!string.IsNullOrEmpty(request.Description))
            plan.Description = request.Description;
        
        if (request.Price.HasValue)
            plan.Price = request.Price.Value;
        
        if (request.BillingCycle.HasValue)
            plan.BillingCycle = (Domain.Enums.BillingCycle)request.BillingCycle.Value;

        // ClearAllowedRole = true -> set về null (tất cả role)
        // AllowedRole có giá trị -> cập nhật role
        // Cả hai đều mặc định -> không thay đổi
        if (request.ClearAllowedRole)
            plan.AllowedRole = null;
        else if (request.AllowedRole != null)
            plan.AllowedRole = string.IsNullOrWhiteSpace(request.AllowedRole) ? null : request.AllowedRole.Trim();

        plan.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Audit log
        await _auditLogService.LogActionAsync(0, "Update", "Plan", plan.Id, oldValues, JsonSerializer.Serialize(plan));

        return MapToPlanDto(plan);
    }

    public async Task DeletePlanAsync(int planId)
    {
        var plan = await _context.Plans.FindAsync(planId);
        if (plan == null)
            throw new KeyNotFoundException($"Plan with ID {planId} not found");

        // Prevent FK violation: plan cannot be removed when any subscription references it
        var hasAnySubscriptions = await _context.Subscriptions
            .AnyAsync(s => s.PlanId == planId);

        if (hasAnySubscriptions)
            throw new InvalidOperationException("Cannot delete plan because it is referenced by existing subscriptions");

        var oldValues = JsonSerializer.Serialize(plan);

        _context.Plans.Remove(plan);
        await _context.SaveChangesAsync();

        // Audit log
        await _auditLogService.LogActionAsync(0, "Delete", "Plan", planId, oldValues, null);
    }

    public async Task<PlanDto> TogglePlanActiveAsync(int planId)
    {
        var plan = await _context.Plans.FindAsync(planId);
        if (plan == null)
            throw new KeyNotFoundException($"Plan with ID {planId} not found");

        var oldValue = plan.IsActive;
        plan.IsActive = !plan.IsActive;
        plan.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Audit log
        await _auditLogService.LogActionAsync(0, "ToggleActive", "Plan", plan.Id, 
            $"IsActive: {oldValue}", $"IsActive: {plan.IsActive}");

        return MapToPlanDto(plan);
    }

    private PlanDto MapToPlanDto(Plan plan)
    {
        return new PlanDto
        {
            Id = plan.Id,
            Name = plan.Name,
            Description = plan.Description,
            Price = plan.Price,
            BillingCycle = plan.BillingCycle.ToString(),
            AllowedRole = plan.AllowedRole,
            Features = plan.Features?.Where(f => f.IsActive).Select(f => new Subscription.Application.DTOs.PlanFeatureDto
            {
                FeatureKey = f.FeatureKey,
                FeatureName = f.FeatureName,
                Value = f.Value,
                Type = f.Type.ToString()
            }).ToList() ?? new List<Subscription.Application.DTOs.PlanFeatureDto>()
        };
    }

    // Plan Feature CRUD Operations
    public async Task<IEnumerable<Subscription.Application.DTOs.Admin.PlanFeatureDto>> GetPlanFeaturesAsync(int planId)
    {
        var planExists = await _context.Plans.AnyAsync(p => p.Id == planId);
        if (!planExists)
            throw new KeyNotFoundException($"Plan with ID {planId} not found");

        var features = await _context.PlanFeatures
            .Where(f => f.PlanId == planId)
            .OrderBy(f => f.Id)
            .ToListAsync();

        return features.Select(MapToPlanFeatureDto);
    }

    public async Task<Subscription.Application.DTOs.Admin.PlanFeatureDto> AddPlanFeatureAsync(int planId, CreatePlanFeatureRequest request)
    {
        // Check if plan exists
        var plan = await _context.Plans.FindAsync(planId);
        if (plan == null)
            throw new KeyNotFoundException($"Plan with ID {planId} not found");

        // Check for duplicate feature key
        var existingFeature = await _context.PlanFeatures
            .FirstOrDefaultAsync(f => f.PlanId == planId && f.FeatureKey == request.FeatureKey);
        
        if (existingFeature != null)
            throw new InvalidOperationException($"Feature with key '{request.FeatureKey}' already exists for this plan");

        var feature = new PlanFeature
        {
            PlanId = planId,
            FeatureKey = request.FeatureKey,
            FeatureName = request.FeatureName,
            Value = request.Value,
            Type = (Domain.Enums.FeatureType)request.Type,
            IsActive = true
        };

        _context.PlanFeatures.Add(feature);
        await _context.SaveChangesAsync();

        // Audit log
        await _auditLogService.LogActionAsync(0, "AddFeature", "PlanFeature", feature.Id, null, JsonSerializer.Serialize(feature));

        return MapToPlanFeatureDto(feature);
    }

    public async Task<Subscription.Application.DTOs.Admin.PlanFeatureDto> UpdatePlanFeatureAsync(int planId, int featureId, UpdatePlanFeatureRequest request)
    {
        var feature = await _context.PlanFeatures
            .FirstOrDefaultAsync(f => f.Id == featureId && f.PlanId == planId);
        
        if (feature == null)
            throw new KeyNotFoundException($"Feature with ID {featureId} not found for plan {planId}");

        var oldValues = JsonSerializer.Serialize(feature);

        if (!string.IsNullOrEmpty(request.FeatureName))
            feature.FeatureName = request.FeatureName;
        
        if (!string.IsNullOrEmpty(request.Value))
            feature.Value = request.Value;
        
        if (request.Type.HasValue)
            feature.Type = (Domain.Enums.FeatureType)request.Type.Value;

        await _context.SaveChangesAsync();

        // Audit log
        await _auditLogService.LogActionAsync(0, "UpdateFeature", "PlanFeature", feature.Id, oldValues, JsonSerializer.Serialize(feature));

        return MapToPlanFeatureDto(feature);
    }

    public async Task DeletePlanFeatureAsync(int planId, int featureId)
    {
        var feature = await _context.PlanFeatures
            .FirstOrDefaultAsync(f => f.Id == featureId && f.PlanId == planId);
        
        if (feature == null)
            throw new KeyNotFoundException($"Feature with ID {featureId} not found for plan {planId}");

        var oldValues = JsonSerializer.Serialize(feature);

        _context.PlanFeatures.Remove(feature);
        await _context.SaveChangesAsync();

        // Audit log
        await _auditLogService.LogActionAsync(0, "DeleteFeature", "PlanFeature", featureId, oldValues, null);
    }

    private Subscription.Application.DTOs.Admin.PlanFeatureDto MapToPlanFeatureDto(PlanFeature feature)
    {
        return new Subscription.Application.DTOs.Admin.PlanFeatureDto
        {
            Id = feature.Id,
            PlanId = feature.PlanId,
            FeatureKey = feature.FeatureKey,
            FeatureName = feature.FeatureName,
            Value = feature.Value,
            Type = feature.Type.ToString(),
            IsActive = feature.IsActive
        };
    }
    public async Task<IEnumerable<AdminSubscriptionDto>> GetAllSubscriptionsAsync(SubscriptionFilter filter)
    {
        var query = _context.Subscriptions
            .Include(s => s.Plan)
            .AsQueryable();

        // Apply filters
        if (filter.UserId.HasValue)
            query = query.Where(s => s.UserId == filter.UserId.Value);

        if (filter.PlanId.HasValue)
            query = query.Where(s => s.PlanId == filter.PlanId.Value);

        if (!string.IsNullOrEmpty(filter.Status) && Enum.TryParse<Domain.Enums.SubscriptionStatus>(filter.Status, out var status))
            query = query.Where(s => s.Status == status);

        if (filter.StartDate.HasValue)
            query = query.Where(s => s.StartDate >= filter.StartDate.Value);

        if (filter.EndDate.HasValue)
            query = query.Where(s => s.EndDate <= filter.EndDate.Value);

        // Pagination
        var subscriptions = await query
            .OrderByDescending(s => s.CreatedAt)
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        var items = subscriptions.Select(MapToAdminSubscriptionDto).ToList();
        await EnrichUserProfilesAsync(items);
        return items;
    }

    public async Task<AdminSubscriptionDto?> GetSubscriptionByIdAsync(int subscriptionId)
    {
        var subscription = await _context.Subscriptions
            .Include(s => s.Plan)
            .FirstOrDefaultAsync(s => s.Id == subscriptionId);

        return subscription == null ? null : MapToAdminSubscriptionDto(subscription);
    }

    private AdminSubscriptionDto MapToAdminSubscriptionDto(UserSubscription s)
    {
        return new AdminSubscriptionDto
        {
            Id = s.Id,
            UserId = s.UserId,
            UserName = "Unknown",
            UserAvatar = string.Empty,
            PlanId = s.PlanId,
            PlanName = s.Plan?.Name ?? "Unknown",
            StartDate = s.StartDate,
            EndDate = s.EndDate,
            Status = s.Status.ToString(),
            PaymentStatus = s.PaymentStatus.ToString(),
            AutoRenew = s.AutoRenew,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt
        };
    }
    public async Task<IEnumerable<AdminSubscriptionDto>> GetUserSubscriptionHistoryAsync(int userId)
    {
        var subscriptions = await _context.Subscriptions
            .Include(s => s.Plan)
            .Where(s => s.UserId == userId)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        return subscriptions.Select(s => new AdminSubscriptionDto
        {
            Id = s.Id,
            UserId = s.UserId,
            UserName = "Unknown",
            UserAvatar = string.Empty,
            PlanId = s.PlanId,
            PlanName = s.Plan?.Name ?? "Unknown",
            StartDate = s.StartDate,
            EndDate = s.EndDate,
            Status = s.Status.ToString(),
            PaymentStatus = s.PaymentStatus.ToString(),
            AutoRenew = s.AutoRenew,
            CreatedAt = s.CreatedAt,
            UpdatedAt = s.UpdatedAt
        });
    }

    private async Task EnrichUserProfilesAsync(List<AdminSubscriptionDto> items)
    {
        var userIds = items.Select(x => x.UserId);
        var profileMap = await _userProfileClient.GetUsersBatchAsync(userIds);

        foreach (var item in items)
        {
            if (!profileMap.TryGetValue(item.UserId, out var profile))
            {
                continue;
            }

            item.UserName = profile.Name;
            item.UserAvatar = profile.Avatar;
        }
    }
    public async Task CancelSubscriptionAsync(int subscriptionId, AdminCancelRequest request)
    {
        var subscription = await _context.Subscriptions.FindAsync(subscriptionId);
        if (subscription == null)
            throw new KeyNotFoundException($"Subscription with ID {subscriptionId} not found");

        var oldStatus = subscription.Status;
        subscription.Status = Domain.Enums.SubscriptionStatus.Cancelled;
        subscription.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Audit log with reason
        await _auditLogService.LogActionAsync(0, "AdminCancel", "Subscription", subscriptionId,
            $"Status: {oldStatus}",
            $"Status: Cancelled, Reason: {request.Reason}, IssueRefund: {request.IssueRefund}");

        // If refund requested, issue refund
        if (request.IssueRefund)
        {
            await IssueRefundAsync(subscriptionId, new RefundRequest 
            { 
                Reason = $"Admin cancelled: {request.Reason}" 
            });
        }

        // TODO: Publish SubscriptionCancelledByAdmin event to RabbitMQ
        // TODO: Send notification to user
    }
    public async Task ExtendSubscriptionAsync(int subscriptionId, ExtendRequest request)
    {
        var subscription = await _context.Subscriptions.FindAsync(subscriptionId);
        if (subscription == null)
            throw new KeyNotFoundException($"Subscription with ID {subscriptionId} not found");

        if (request.Months <= 0)
            throw new ArgumentException("Months must be greater than 0");

        var oldEndDate = subscription.EndDate;
        subscription.EndDate = subscription.EndDate.AddMonths(request.Months);
        subscription.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Audit log
        await _auditLogService.LogActionAsync(0, "Extend", "Subscription", subscriptionId, 
            $"EndDate: {oldEndDate:yyyy-MM-dd}", 
            $"EndDate: {subscription.EndDate:yyyy-MM-dd}, ExtendedBy: {request.Months} months");

        // TODO: Send notification to user about subscription extension
    }
    public async Task IssueRefundAsync(int subscriptionId, RefundRequest request)
    {
        var subscription = await _context.Subscriptions.FindAsync(subscriptionId);
        if (subscription == null)
            throw new KeyNotFoundException($"Subscription with ID {subscriptionId} not found");

        var oldPaymentStatus = subscription.PaymentStatus;
        subscription.PaymentStatus = Domain.Enums.PaymentStatus.Refunded;
        subscription.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Audit log
        await _auditLogService.LogActionAsync(0, "Refund", "Subscription", subscriptionId,
            $"PaymentStatus: {oldPaymentStatus}",
            $"PaymentStatus: Refunded, Reason: {request.Reason}");

        // TODO: Publish PaymentRefundRequested event to Payment Service via RabbitMQ
        // TODO: Send notification to user
    }
    // Analytics methods - delegated to AnalyticsService, kept here for interface compliance
    public Task<AnalyticsOverviewDto> GetAnalyticsOverviewAsync(DateRangeFilter filter)
    {
        throw new NotSupportedException("Use AnalyticsService directly for analytics operations");
    }
    
    public Task<RevenueAnalyticsDto> GetRevenueAnalyticsAsync(DateRangeFilter filter)
    {
        throw new NotSupportedException("Use AnalyticsService directly for analytics operations");
    }
    
    public Task<IEnumerable<SubscriptionCountByPlanDto>> GetSubscriptionsByPlanAsync()
    {
        throw new NotSupportedException("Use AnalyticsService directly for analytics operations");
    }
    
    public Task<ChurnAnalyticsDto> GetChurnRateAsync(DateRangeFilter filter)
    {
        throw new NotSupportedException("Use AnalyticsService directly for analytics operations");
    }
}

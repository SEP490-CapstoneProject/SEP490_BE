using Microsoft.Extensions.Logging;
using Subscription.Application.DTOs;
using Subscription.Application.Events;
using Subscription.Application.Interfaces;
using Subscription.Domain.Entities;
using Subscription.Domain.Enums;

namespace Subscription.Application.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly IPlanRepository _planRepository;
    private readonly ISubscriptionRepository _subscriptionRepository;
    private readonly IProcessedEventRepository _processedEventRepository;
    private readonly IRedisService _redisService;
    private readonly IRabbitMQPublisher _rabbitMQPublisher;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(
        IPlanRepository planRepository,
        ISubscriptionRepository subscriptionRepository,
        IProcessedEventRepository processedEventRepository,
        IRedisService redisService,
        IRabbitMQPublisher rabbitMQPublisher,
        ILogger<SubscriptionService> logger)
    {
        _planRepository = planRepository;
        _subscriptionRepository = subscriptionRepository;
        _processedEventRepository = processedEventRepository;
        _redisService = redisService;
        _rabbitMQPublisher = rabbitMQPublisher;
        _logger = logger;
    }

    public async Task<IEnumerable<PlanDto>> GetAllPlansAsync()
    {
        var plans = await _planRepository.GetAllActiveAsync();
        return plans.Select(MapToPlanDto);
    }

    public async Task<IEnumerable<PlanDto>> GetPlansByRoleAsync(string role)
    {
        var plans = await _planRepository.GetActiveByRoleAsync(role);
        return plans.Select(MapToPlanDto);
    }

    public async Task<PlanDto?> GetPlanByIdAsync(int planId)
    {
        var plan = await _planRepository.GetByIdWithFeaturesAsync(planId);
        return plan == null ? null : MapToPlanDto(plan);
    }

    public async Task<SubscriptionDto> SubscribeAsync(int userId, SubscribeRequest request)
    {
        var plan = await _planRepository.GetByIdWithFeaturesAsync(request.PlanId)
            ?? throw new ArgumentException("Plan not found");

        // Check if user already has active subscription
        var existing = await _subscriptionRepository.GetActiveByUserIdAsync(userId);
        if (existing != null)
        {
            throw new InvalidOperationException("User already has an active subscription. Use upgrade instead.");
        }

        // Check for existing pending subscription for same plan
        var pendingSubscription = await _subscriptionRepository.GetPendingByUserAndPlanAsync(userId, request.PlanId);
        if (pendingSubscription != null)
        {
            _logger.LogInformation("Returning existing pending subscription {SubscriptionId} for user {UserId}", 
                pendingSubscription.Id, userId);

            // Update StartDate and EndDate to current time
            pendingSubscription.StartDate = DateTime.UtcNow;
            pendingSubscription.EndDate = plan.BillingCycle == BillingCycle.Monthly 
                ? DateTime.UtcNow.AddMonths(1) 
                : DateTime.UtcNow.AddYears(1);
            pendingSubscription.UpdatedAt = DateTime.UtcNow;

            await _subscriptionRepository.UpdateAsync(pendingSubscription);

            return MapToSubscriptionDto(pendingSubscription, plan.Name);
        }

        var subscription = new UserSubscription
        {
            UserId = userId,
            PlanId = request.PlanId,
            StartDate = DateTime.UtcNow,
            EndDate = plan.BillingCycle == BillingCycle.Monthly 
                ? DateTime.UtcNow.AddMonths(1) 
                : DateTime.UtcNow.AddYears(1),
            Status = SubscriptionStatus.Pending,
            PaymentStatus = PaymentStatus.Pending,
            AutoRenew = request.AutoRenew
        };

        subscription = await _subscriptionRepository.CreateAsync(subscription);
        _logger.LogInformation("Created subscription {SubscriptionId} for user {UserId}, plan {PlanId}. Awaiting payment.", 
            subscription.Id, userId, request.PlanId);

        // NOTE: No longer auto-activating! User must call Payment Service with this SubscriptionId
        // Payment Service webhook will send event to activate this subscription

        return MapToSubscriptionDto(subscription, plan.Name);
    }

    public async Task<SubscriptionDto> UpgradeAsync(int userId, UpgradeRequest request)
    {
        var currentSubscription = await _subscriptionRepository.GetActiveByUserIdAsync(userId)
            ?? throw new InvalidOperationException("No active subscription to upgrade");

        var newPlan = await _planRepository.GetByIdWithFeaturesAsync(request.NewPlanId)
            ?? throw new ArgumentException("New plan not found");

        var oldPlanId = currentSubscription.PlanId;
        currentSubscription.PlanId = request.NewPlanId;
        currentSubscription.UpdatedAt = DateTime.UtcNow;
        
        await _subscriptionRepository.UpdateAsync(currentSubscription);

        // Invalidate and refresh Redis cache
        await _redisService.InvalidateUserCacheAsync(userId);
        await WriteEntitlementsToRedisAsync(currentSubscription);

        // Publish upgrade event
        var @event = new SubscriptionUpgradedEvent
        {
            UserId = userId,
            SubscriptionId = currentSubscription.Id,
            OldPlanId = oldPlanId,
            NewPlanId = request.NewPlanId,
            NewPlanName = newPlan.Name,
            Features = GetFeaturesDictionary(newPlan.Features),
            ExpiredAt = currentSubscription.EndDate
        };
        await _rabbitMQPublisher.PublishAsync(@event);

        _logger.LogInformation("Upgraded subscription {SubscriptionId} from plan {OldPlanId} to {NewPlanId}", 
            currentSubscription.Id, oldPlanId, request.NewPlanId);

        return MapToSubscriptionDto(currentSubscription, newPlan.Name);
    }

    public async Task CancelSubscriptionAsync(int userId, CancelRequest request)
    {
        var subscription = await _subscriptionRepository.GetActiveByUserIdAsync(userId)
            ?? throw new InvalidOperationException("No active subscription to cancel");

        subscription.Status = SubscriptionStatus.Cancelled;
        subscription.UpdatedAt = DateTime.UtcNow;
        await _subscriptionRepository.UpdateAsync(subscription);

        // Remove from Redis
        await _redisService.RemoveEntitlementsAsync(userId);

        // Publish cancel event
        var @event = new SubscriptionCancelledEvent
        {
            UserId = userId,
            SubscriptionId = subscription.Id,
            Reason = request.Reason ?? "User requested cancellation"
        };
        await _rabbitMQPublisher.PublishAsync(@event);

        _logger.LogInformation("Cancelled subscription {SubscriptionId} for user {UserId}", 
            subscription.Id, userId);
    }

    public async Task<SubscriptionDto?> GetActiveSubscriptionAsync(int userId)
    {
        var subscription = await _subscriptionRepository.GetActiveByUserIdAsync(userId);
        if (subscription == null) return null;
        
        return MapToSubscriptionDto(subscription, subscription.Plan?.Name ?? "Unknown");
    }

    public async Task<EntitlementsDto?> GetEntitlementsAsync(int userId, string? role = null)
    {
        // 3-level fallback
        // Level 1: Redis cache
        var cached = await _redisService.GetEntitlementsAsync(userId);
        if (cached != null)
        {
            return cached;
        }

        // Level 2: Database
        var subscription = await _subscriptionRepository.GetActiveByUserIdAsync(userId);
        if (subscription != null)
        {
            var entitlements = BuildEntitlements(subscription);
            // Cache for next time
            await _redisService.SetEntitlementsAsync(userId, entitlements);
            return entitlements;
        }

        // Level 3: Default free tier theo role
        return await GetDefaultFreeTierEntitlementsAsync(role);
    }

    public async Task ActivateSubscriptionAsync(int subscriptionId, string eventId)
    {
        // Idempotency check
        if (await _processedEventRepository.ExistsAsync(eventId))
        {
            _logger.LogWarning("Event {EventId} already processed, skipping", eventId);
            return;
        }

        var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId)
            ?? throw new ArgumentException("Subscription not found");

        // Update subscription status
        subscription.Status = SubscriptionStatus.Active;
        subscription.PaymentStatus = PaymentStatus.Paid;
        subscription.UpdatedAt = DateTime.UtcNow;
        await _subscriptionRepository.UpdateAsync(subscription);

        // Write entitlements to Redis
        await WriteEntitlementsToRedisAsync(subscription);

        // Mark event as processed
        await _processedEventRepository.AddAsync(new ProcessedEvent
        {
            EventId = eventId,
            EventType = "payment.succeeded"
        });

        // Publish activation event
        var @event = new SubscriptionActivatedEvent
        {
            UserId = subscription.UserId,
            SubscriptionId = subscription.Id,
            PlanId = subscription.PlanId,
            PlanName = subscription.Plan?.Name ?? "Unknown",
            Features = subscription.Plan != null 
                ? GetFeaturesDictionary(subscription.Plan.Features) 
                : new Dictionary<string, object>(),
            StartDate = subscription.StartDate,
            ExpiredAt = subscription.EndDate
        };
        await _rabbitMQPublisher.PublishAsync(@event);

        _logger.LogInformation("Activated subscription {SubscriptionId} for user {UserId}", 
            subscriptionId, subscription.UserId);
    }

    public async Task ExpireSubscriptionAsync(int subscriptionId)
    {
        var subscription = await _subscriptionRepository.GetByIdAsync(subscriptionId)
            ?? throw new ArgumentException("Subscription not found");

        subscription.Status = SubscriptionStatus.Expired;
        subscription.UpdatedAt = DateTime.UtcNow;
        await _subscriptionRepository.UpdateAsync(subscription);

        // Remove from Redis
        await _redisService.RemoveEntitlementsAsync(subscription.UserId);

        // Publish expiration event
        var @event = new SubscriptionExpiredEvent
        {
            UserId = subscription.UserId,
            SubscriptionId = subscription.Id
        };
        await _rabbitMQPublisher.PublishAsync(@event);

        _logger.LogInformation("Expired subscription {SubscriptionId} for user {UserId}", 
            subscriptionId, subscription.UserId);
    }

    private async Task WriteEntitlementsToRedisAsync(UserSubscription subscription)
    {
        var entitlements = BuildEntitlements(subscription);
        var ttl = (subscription.EndDate - DateTime.UtcNow).Add(TimeSpan.FromDays(1));
        await _redisService.SetEntitlementsAsync(subscription.UserId, entitlements, ttl);
    }

    private EntitlementsDto BuildEntitlements(UserSubscription subscription)
    {
        return new EntitlementsDto
        {
            Version = 1,
            PlanId = subscription.PlanId,
            PlanName = subscription.Plan?.Name ?? "Unknown",
            Features = subscription.Plan != null 
                ? GetFeaturesDictionary(subscription.Plan.Features) 
                : new Dictionary<string, object>(),
            ExpiredAt = subscription.EndDate
        };
    }

    private static Dictionary<string, object> GetFeaturesDictionary(ICollection<PlanFeature> features)
    {
        var dict = new Dictionary<string, object>();
        foreach (var feature in features.Where(f => f.IsActive))
        {
            object value = feature.Type switch
            {
                FeatureType.Boolean => bool.Parse(feature.Value),
                FeatureType.Number => int.Parse(feature.Value),
                _ => feature.Value
            };
            dict[feature.FeatureKey] = value;
        }
        return dict;
    }

    private async Task<EntitlementsDto> GetDefaultFreeTierEntitlementsAsync(string? role)
    {
        // Nếu có role, tìm free plan tương ứng từ DB
        if (!string.IsNullOrWhiteSpace(role))
        {
            var freePlan = await _planRepository.GetFreePlanByRoleAsync(role);
            if (freePlan != null)
            {
                return new EntitlementsDto
                {
                    Version = 1,
                    PlanId = freePlan.Id,
                    PlanName = freePlan.Name,
                    Features = GetFeaturesDictionary(freePlan.Features),
                    ExpiredAt = DateTime.UtcNow.AddYears(10)
                };
            }

            // Có role nhưng không có free plan cho role này → trả về entitlements rỗng
            _logger.LogWarning("No free plan found for role '{Role}'. Returning empty entitlements.", role);
            return new EntitlementsDto
            {
                Version = 1,
                PlanId = 0,
                PlanName = "None",
                Features = new Dictionary<string, object>(),
                ExpiredAt = DateTime.UtcNow.AddYears(10)
            };
        }

        // Fallback cứng chỉ khi caller không truyền role (legacy/anonymous call)
        _logger.LogWarning("GetEntitlementsAsync called without role. Returning default Talent free tier.");
        return new EntitlementsDto
        {
            Version = 1,
            PlanId = 1,
            PlanName = "Free",
            Features = new Dictionary<string, object>
            {
                { "MAX_APPLY", 5 },
                { "MAX_PORTFOLIOS", 1 },
                { "AI_MATCHING", false },
                { "BOOST_PROFILE", false },
                { "COMPLIMENT_ACCESS", false }
            },
            ExpiredAt = DateTime.UtcNow.AddYears(10)
        };
    }

    private static PlanDto MapToPlanDto(Plan plan)
    {
        return new PlanDto
        {
            Id = plan.Id,
            Name = plan.Name,
            Description = plan.Description,
            Price = plan.Price,
            BillingCycle = plan.BillingCycle.ToString(),
            AllowedRole = plan.AllowedRole,
            Features = plan.Features.Where(f => f.IsActive).Select(f => new PlanFeatureDto
            {
                FeatureKey = f.FeatureKey,
                FeatureName = f.FeatureName,
                Value = f.Value,
                Type = f.Type.ToString()
            }).ToList()
        };
    }

    private static SubscriptionDto MapToSubscriptionDto(UserSubscription subscription, string planName)
    {
        return new SubscriptionDto
        {
            Id = subscription.Id,
            UserId = subscription.UserId,
            PlanId = subscription.PlanId,
            PlanName = planName,
            StartDate = subscription.StartDate,
            EndDate = subscription.EndDate,
            Status = subscription.Status.ToString(),
            PaymentStatus = subscription.PaymentStatus.ToString(),
            AutoRenew = subscription.AutoRenew,
            CreatedAt = subscription.CreatedAt
        };
    }
}

using Microsoft.Extensions.Logging;
using Moq;
using Subscription.Application.DTOs;
using Subscription.Application.Interfaces;
using Subscription.Application.Services;
using Subscription.Domain.Entities;
using Subscription.Domain.Enums;

namespace Subscription.Tests;

public class FeatureVerificationServiceTests
{
    private readonly Mock<ISubscriptionRepository> _subscriptionRepository = new();
    private readonly Mock<IPlanRepository> _planRepository = new();
    private readonly Mock<IRedisService> _redisService = new();
    private readonly Mock<ILogger<FeatureVerificationService>> _logger = new();

    private FeatureVerificationService CreateService(
        UserSubscription? subscription = null,
        EntitlementsDto? cached = null,
        IEnumerable<Plan>? plans = null)
    {
        _subscriptionRepository.Reset();
        _planRepository.Reset();
        _redisService.Reset();

        _subscriptionRepository
            .Setup(r => r.GetActiveByUserIdAsync(It.IsAny<int>()))
            .ReturnsAsync(subscription);

        _planRepository
            .Setup(r => r.GetAllActiveAsync())
            .ReturnsAsync(plans ?? new List<Plan>());

        _redisService
            .Setup(r => r.GetEntitlementsAsync(It.IsAny<int>()))
            .ReturnsAsync(cached);

        return new FeatureVerificationService(
            _subscriptionRepository.Object,
            _planRepository.Object,
            _redisService.Object,
            _logger.Object);
    }

    [Fact]
    public async Task HasFeatureAccessAsync_BooleanFalse_ReturnsFalse()
    {
        var subscription = BuildSubscription(new PlanFeature
        {
            FeatureKey = "AI_MATCHING",
            FeatureName = "AI Matching",
            Value = "false",
            Type = FeatureType.Boolean,
            IsActive = true
        });

        var service = CreateService(subscription);

        var result = await service.HasFeatureAccessAsync(1, "AI_MATCHING");

        Assert.False(result);
    }

    [Fact]
    public async Task GetFeatureValueAsync_NumberFeature_ReturnsValue()
    {
        var subscription = BuildSubscription(new PlanFeature
        {
            FeatureKey = "MAX_APPLY",
            FeatureName = "Max Applications",
            Value = "5",
            Type = FeatureType.Number,
            IsActive = true
        });

        var service = CreateService(subscription);

        var result = await service.GetFeatureValueAsync(1, "MAX_APPLY");

        Assert.Equal("5", result);
    }

    [Fact]
    public async Task CanPerformActionAsync_Unlimited_ReturnsTrue()
    {
        var subscription = BuildSubscription(new PlanFeature
        {
            FeatureKey = "MAX_APPLY",
            FeatureName = "Max Applications",
            Value = "-1",
            Type = FeatureType.Number,
            IsActive = true
        });

        var service = CreateService(subscription);

        var result = await service.CanPerformActionAsync(1, "MAX_APPLY", currentCount: 999);

        Assert.True(result);
    }

    [Fact]
    public async Task GetFeatureEntitlementAsync_ReturnsDto()
    {
        var subscription = BuildSubscription(new PlanFeature
        {
            FeatureKey = "MAX_PORTFOLIOS",
            FeatureName = "Max Portfolios",
            Value = "3",
            Type = FeatureType.Number,
            IsActive = true
        });

        var service = CreateService(subscription);

        var result = await service.GetFeatureEntitlementAsync(1, "MAX_PORTFOLIOS");

        Assert.NotNull(result);
        Assert.Equal("MAX_PORTFOLIOS", result!.FeatureKey);
        Assert.Equal("Max Portfolios", result.FeatureName);
        Assert.Equal("3", result.Value);
        Assert.Equal(FeatureType.Number.ToString(), result.Type);
        Assert.True(result.IsActive);
    }

    [Fact]
    public async Task GetUserEntitlementsAsync_UsesCache()
    {
        var cached = new EntitlementsDto
        {
            Version = 1,
            PlanId = 2,
            PlanName = "Pro",
            Features = new Dictionary<string, object>
            {
                { "MAX_APPLY", 20 }
            },
            ExpiredAt = DateTime.UtcNow.AddDays(30)
        };

        var service = CreateService(cached: cached);

        var result = await service.GetUserEntitlementsAsync(1);

        Assert.Equal("20", result["MAX_APPLY"]);
        _subscriptionRepository.Verify(r => r.GetActiveByUserIdAsync(It.IsAny<int>()), Times.Never);
    }

    private static UserSubscription BuildSubscription(params PlanFeature[] features)
    {
        var plan = new Plan
        {
            Id = 1,
            Name = "Free",
            Features = features.ToList()
        };

        return new UserSubscription
        {
            Id = 1,
            UserId = 1,
            PlanId = plan.Id,
            Status = SubscriptionStatus.Active,
            StartDate = DateTime.UtcNow.AddDays(-1),
            EndDate = DateTime.UtcNow.AddDays(30),
            Plan = plan
        };
    }
}

using System;
using Moq;
using Xunit;
using Subscription.Application.Interfaces;
using Subscription.Application.Services;
using Subscription.Domain.Entities;
using Subscription.Domain.Enums;
using Microsoft.Extensions.Logging;
using Subscription.Application.DTOs;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Subscription.Tests.Services;

public class FeatureVerificationServiceTests
{
    private readonly Mock<ISubscriptionRepository> _mockSubscriptionRepository;
    private readonly Mock<IPlanRepository> _mockPlanRepository;
    private readonly Mock<IRedisService> _mockRedisService;
    private readonly Mock<ILogger<FeatureVerificationService>> _mockLogger;
    private readonly IFeatureVerificationService _service;

    public FeatureVerificationServiceTests()
    {
        _mockSubscriptionRepository = new Mock<ISubscriptionRepository>();
        _mockPlanRepository = new Mock<IPlanRepository>();
        _mockRedisService = new Mock<IRedisService>();
        _mockLogger = new Mock<ILogger<FeatureVerificationService>>();

        _service = new FeatureVerificationService(
            _mockSubscriptionRepository.Object,
            _mockPlanRepository.Object,
            _mockRedisService.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task HasFeatureAccessAsync_WithValidFeature_ReturnsTrue()
    {
        // Arrange
        int userId = 123;
        string featureKey = "MAX_APPLY";
        var plan = CreateTestPlan(PlanType.Pro);
        var subscription = CreateTestSubscription(userId, plan, SubscriptionStatus.Active);

        _mockSubscriptionRepository
            .Setup(r => r.GetActiveByUserIdAsync(userId))
            .ReturnsAsync(subscription);
        _mockRedisService.Setup(r => r.GetEntitlementsAsync(userId)).ReturnsAsync((EntitlementsDto?)null);

        // Act
        var result = await _service.HasFeatureAccessAsync(userId, featureKey);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task HasFeatureAccessAsync_WithInvalidFeature_ReturnsFalse()
    {
        // Arrange
        int userId = 123;
        string featureKey = "INVALID_FEATURE";
        var plan = CreateTestPlan(PlanType.Pro);
        var subscription = CreateTestSubscription(userId, plan, SubscriptionStatus.Active);

        _mockSubscriptionRepository
            .Setup(r => r.GetActiveByUserIdAsync(userId))
            .ReturnsAsync(subscription);
        _mockRedisService.Setup(r => r.GetEntitlementsAsync(userId)).ReturnsAsync((EntitlementsDto?)null);

        // Act
        var result = await _service.HasFeatureAccessAsync(userId, featureKey);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetFeatureValueAsync_WithFreePlan_Returns5ForMaxApply()
    {
        // Arrange
        int userId = 123;
        string featureKey = "MAX_APPLY";
        var plan = CreateTestPlan(PlanType.Free);
        var subscription = CreateTestSubscription(userId, plan, SubscriptionStatus.Active);

        _mockSubscriptionRepository
            .Setup(r => r.GetActiveByUserIdAsync(userId))
            .ReturnsAsync(subscription);
        _mockRedisService.Setup(r => r.GetEntitlementsAsync(userId)).ReturnsAsync((EntitlementsDto?)null);

        // Act
        var result = await _service.GetFeatureValueAsync(userId, featureKey);

        // Assert
        Assert.Equal("5", result);
    }

    [Fact]
    public async Task GetFeatureValueAsync_WithProPlan_Returns20ForMaxApply()
    {
        // Arrange
        int userId = 123;
        string featureKey = "MAX_APPLY";
        var plan = CreateTestPlan(PlanType.Pro);
        var subscription = CreateTestSubscription(userId, plan, SubscriptionStatus.Active);

        _mockSubscriptionRepository
            .Setup(r => r.GetActiveByUserIdAsync(userId))
            .ReturnsAsync(subscription);
        _mockRedisService.Setup(r => r.GetEntitlementsAsync(userId)).ReturnsAsync((EntitlementsDto?)null);

        // Act
        var result = await _service.GetFeatureValueAsync(userId, featureKey);

        // Assert
        Assert.Equal("20", result);
    }

    [Fact]
    public async Task CanPerformActionAsync_BelowLimit_ReturnsTrue()
    {
        // Arrange
        int userId = 123;
        int currentCount = 3;
        string actionKey = "MAX_APPLY";
        var plan = CreateTestPlan(PlanType.Pro); // Limit is 20
        var subscription = CreateTestSubscription(userId, plan, SubscriptionStatus.Active);

        _mockSubscriptionRepository
            .Setup(r => r.GetActiveByUserIdAsync(userId))
            .ReturnsAsync(subscription);
        _mockRedisService.Setup(r => r.GetEntitlementsAsync(userId)).ReturnsAsync((EntitlementsDto?)null);

        // Act
        var result = await _service.CanPerformActionAsync(userId, actionKey, currentCount);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanPerformActionAsync_WithUnlimitedFeature_ReturnsTrue()
    {
        // Arrange
        int userId = 123;
        int currentCount = 999;
        string actionKey = "MAX_APPLY";
        var plan = CreateTestPlan(PlanType.Premium); // Limit is -1 (unlimited)
        var subscription = CreateTestSubscription(userId, plan, SubscriptionStatus.Active);

        _mockSubscriptionRepository
            .Setup(r => r.GetActiveByUserIdAsync(userId))
            .ReturnsAsync(subscription);
        _mockRedisService.Setup(r => r.GetEntitlementsAsync(userId)).ReturnsAsync((EntitlementsDto?)null);

        // Act
        var result = await _service.CanPerformActionAsync(userId, actionKey, currentCount);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task GetUserEntitlementsAsync_WithCache_ReturnsCachedData()
    {
        // Arrange
        int userId = 123;
        var cachedEntitlements = new EntitlementsDto
        {
            Version = 1,
            PlanId = 2,
            PlanName = "Pro",
            Features = new Dictionary<string, object> { { "MAX_APPLY", "20" } },
            ExpiredAt = DateTime.UtcNow.AddMonths(1)
        };

        _mockRedisService
            .Setup(r => r.GetEntitlementsAsync(userId))
            .ReturnsAsync(cachedEntitlements);

        // Act
        var result = await _service.GetUserEntitlementsAsync(userId);

        // Assert
        Assert.NotEmpty(result);
        Assert.Equal("20", result["MAX_APPLY"]);
    }

    private enum PlanType { Free, Pro, Premium }

    private Plan CreateTestPlan(PlanType planType)
    {
        var features = planType switch
        {
            PlanType.Free => new List<PlanFeature>
            {
                new() { FeatureKey = "MAX_APPLY", FeatureName = "Max Applications", Value = "5", Type = FeatureType.Number, IsActive = true },
                new() { FeatureKey = "MAX_PORTFOLIOS", FeatureName = "Max Portfolios", Value = "1", Type = FeatureType.Number, IsActive = true },
                new() { FeatureKey = "AI_MATCHING", FeatureName = "AI Matching", Value = "false", Type = FeatureType.Boolean, IsActive = true }
            },
            PlanType.Pro => new List<PlanFeature>
            {
                new() { FeatureKey = "MAX_APPLY", FeatureName = "Max Applications", Value = "20", Type = FeatureType.Number, IsActive = true },
                new() { FeatureKey = "MAX_PORTFOLIOS", FeatureName = "Max Portfolios", Value = "5", Type = FeatureType.Number, IsActive = true },
                new() { FeatureKey = "AI_MATCHING", FeatureName = "AI Matching", Value = "true", Type = FeatureType.Boolean, IsActive = true }
            },
            PlanType.Premium => new List<PlanFeature>
            {
                new() { FeatureKey = "MAX_APPLY", FeatureName = "Max Applications", Value = "-1", Type = FeatureType.Number, IsActive = true },
                new() { FeatureKey = "MAX_PORTFOLIOS", FeatureName = "Max Portfolios", Value = "-1", Type = FeatureType.Number, IsActive = true },
                new() { FeatureKey = "AI_MATCHING", FeatureName = "AI Matching", Value = "true", Type = FeatureType.Boolean, IsActive = true }
            },
            _ => new List<PlanFeature>()
        };

        return new Plan
        {
            Id = (int)planType + 1,
            Name = planType.ToString(),
            Description = $"{planType} Plan",
            Price = planType == PlanType.Free ? 0 : (planType == PlanType.Pro ? 99 : 199),
            BillingCycle = BillingCycle.Monthly,
            IsActive = true,
            AllowedRole = "1",
            CreatedAt = DateTime.UtcNow,
            Features = features
        };
    }

    private UserSubscription CreateTestSubscription(int userId, Plan plan, SubscriptionStatus status)
    {
        return new UserSubscription
        {
            Id = 1,
            UserId = userId,
            PlanId = plan.Id,
            Plan = plan,
            StartDate = DateTime.UtcNow,
            EndDate = DateTime.UtcNow.AddMonths(1),
            Status = status,
            PaymentStatus = PaymentStatus.Paid,
            AutoRenew = true,
            CreatedAt = DateTime.UtcNow
        };
    }
}

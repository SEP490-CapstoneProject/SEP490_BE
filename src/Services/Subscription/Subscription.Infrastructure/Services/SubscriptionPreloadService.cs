using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Subscription.Application.Interfaces;

namespace Subscription.Infrastructure.Services;

public class SubscriptionPreloadService : IHostedService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SubscriptionPreloadService> _logger;

    public SubscriptionPreloadService(
        IServiceScopeFactory scopeFactory,
        ILogger<SubscriptionPreloadService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Starting subscription preload...");

            // Create a scope to resolve scoped services
            using var scope = _scopeFactory.CreateScope();
            var subscriptionRepository = scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();
            var planRepository = scope.ServiceProvider.GetRequiredService<IPlanRepository>();
            var redisService = scope.ServiceProvider.GetRequiredService<IRedisService>();

            // Get active subscriptions
            var activeSubscriptions = (await subscriptionRepository.GetActiveSubscriptionsAsync()).ToList();
            
            _logger.LogInformation("Found {Count} active subscriptions to preload", activeSubscriptions.Count);

            var preloadedCount = 0;
            foreach (var subscription in activeSubscriptions)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                try
                {
                    var plan = await planRepository.GetByIdAsync(subscription.PlanId);
                    if (plan == null) continue;

                    var features = plan.Features.ToDictionary(
                        f => f.FeatureKey,
                        f => (object)(f.Type == Domain.Enums.FeatureType.Number 
                            ? int.Parse(f.Value) 
                            : (f.Type == Domain.Enums.FeatureType.Boolean 
                                ? bool.Parse(f.Value) 
                                : f.Value))
                    );

                    var ttl = subscription.EndDate > DateTime.UtcNow
                        ? subscription.EndDate - DateTime.UtcNow + TimeSpan.FromDays(1)
                        : TimeSpan.FromDays(1);

                    await redisService.SetEntitlementsAsync(
                        subscription.UserId,
                        new Application.DTOs.EntitlementsDto
                        {
                            Version = 1,
                            PlanId = plan.Id,
                            PlanName = plan.Name,
                            Features = features,
                            ExpiredAt = subscription.EndDate
                        },
                        ttl);

                    preloadedCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to preload subscription {SubscriptionId} for user {UserId}", 
                        subscription.Id, subscription.UserId);
                }
            }

            _logger.LogInformation("Subscription preload complete. Loaded {Count} subscriptions", preloadedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Subscription preload failed");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}

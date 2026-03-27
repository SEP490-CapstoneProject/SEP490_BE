using System.Security.Claims;
using System.Text.Json;
using Subscription.Application.Attributes;
using Subscription.Application.Interfaces;

namespace Subscription.API.Middleware;

public class FeatureAuthorizationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<FeatureAuthorizationMiddleware> _logger;

    public FeatureAuthorizationMiddleware(RequestDelegate next, ILogger<FeatureAuthorizationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IRedisService redisService)
    {
        var endpoint = context.GetEndpoint();
        var featureAttr = endpoint?.Metadata.GetMetadata<RequireFeatureAttribute>();

        if (featureAttr != null)
        {
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier) 
                ?? context.User.FindFirst("sub") 
                ?? context.User.FindFirst("userId");

            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new 
                { 
                    error = "UNAUTHORIZED",
                    message = "User ID not found in token"
                });
                return;
            }

            var entitlements = await redisService.GetEntitlementsAsync(userId);
            if (entitlements == null)
            {
                _logger.LogWarning("No entitlements found for user {UserId}, using default", userId);
                // Allow through with default tier (will be checked in service)
            }
            else if (entitlements.Features.TryGetValue(featureAttr.FeatureKey, out var featureValue))
            {
                // For boolean features, check if enabled
                if (featureValue is bool boolValue && !boolValue)
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(new
                    {
                        error = "FEATURE_NOT_AVAILABLE",
                        message = $"Feature '{featureAttr.FeatureKey}' is not available in your plan",
                        upgradeUrl = "/api/plans"
                    });
                    return;
                }

                // For number features with increment, check quota
                if (featureValue is int limit && featureAttr.IncrementUsage)
                {
                    var (success, currentUsage) = await redisService.TryIncrementUsageAsync(userId, featureAttr.FeatureKey, limit);
                    if (!success)
                    {
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsJsonAsync(new
                        {
                            error = "QUOTA_EXCEEDED",
                            message = $"You have reached your {featureAttr.FeatureKey} limit ({limit}). Upgrade to get more.",
                            currentUsage = limit,
                            limit,
                            upgradeUrl = "/api/plans"
                        });
                        return;
                    }

                    // Store in context for potential rollback
                    context.Items["FeatureUsageIncremented"] = featureAttr.FeatureKey;
                    context.Items["UserId"] = userId;
                }
            }
        }

        await _next(context);

        // Rollback on failure (5xx)
        if (context.Response.StatusCode >= 500 && context.Items.ContainsKey("FeatureUsageIncremented"))
        {
            var featureKey = context.Items["FeatureUsageIncremented"]?.ToString();
            var userId = context.Items["UserId"] as int?;
            if (featureKey != null && userId != null)
            {
                _logger.LogInformation("Rolling back usage for feature {FeatureKey} for user {UserId}", featureKey, userId);
                await redisService.DecrementUsageAsync(userId.Value, featureKey);
            }
        }
    }
}

public static class FeatureAuthorizationExtensions
{
    public static IApplicationBuilder UseFeatureAuthorization(this IApplicationBuilder app)
    {
        return app.UseMiddleware<FeatureAuthorizationMiddleware>();
    }
}

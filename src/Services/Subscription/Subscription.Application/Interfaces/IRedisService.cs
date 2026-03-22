using Subscription.Application.DTOs;

namespace Subscription.Application.Interfaces;

public interface IRedisService
{
    // Entitlements
    Task SetEntitlementsAsync(int userId, EntitlementsDto entitlements, TimeSpan? ttl = null);
    Task<EntitlementsDto?> GetEntitlementsAsync(int userId);
    Task RemoveEntitlementsAsync(int userId);
    Task InvalidateUserCacheAsync(int userId);
    
    // Usage counters with atomic Lua script
    Task<(bool success, int currentUsage)> TryIncrementUsageAsync(int userId, string featureKey, int limit);
    Task DecrementUsageAsync(int userId, string featureKey);
    Task<int> GetUsageAsync(int userId, string featureKey);
    Task ResetUsageAsync(int userId, string featureKey);
    
    // Last event tracking for out-of-order detection
    Task SetLastEventTimeAsync(int userId, DateTime timestamp);
    Task<DateTime?> GetLastEventTimeAsync(int userId);
    
    // Health check
    Task<bool> PingAsync();
}

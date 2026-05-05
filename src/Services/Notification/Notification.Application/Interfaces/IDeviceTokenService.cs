using Notification.Domain.Entities;

namespace Notification.Application.Interfaces
{
    public interface IDeviceTokenService
    {
        Task<bool> RegisterTokenAsync(string userId, string deviceToken, string deviceType = "Android", string? appVersion = null);
        Task<bool> UnregisterTokenAsync(string deviceToken);
        Task<List<DeviceTokenEntity>> GetActiveTokensForUserAsync(string userId);
        Task<bool> DeactivateTokenAsync(string deviceToken);
        Task<bool> UpdateLastUsedAsync(string deviceToken);
        Task CleanupInactiveTokensAsync(int inactiveDaysThreshold = 30);
    }
}

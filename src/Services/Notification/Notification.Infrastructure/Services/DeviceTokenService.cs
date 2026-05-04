using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Notification.Domain.Entities;
using Notification.Infrastructure.Data;

namespace Notification.Infrastructure.Services
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

    public class DeviceTokenService : IDeviceTokenService
    {
        private readonly NotificationDbContext _dbContext;
        private readonly ILogger<DeviceTokenService> _logger;

        public DeviceTokenService(NotificationDbContext dbContext, ILogger<DeviceTokenService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<bool> RegisterTokenAsync(string userId, string deviceToken, string deviceType = "Android", string appVersion = null)
        {
            try
            {
                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(deviceToken))
                {
                    _logger.LogWarning("Invalid userId or deviceToken for registration");
                    return false;
                }

                // Check if token already exists
                var existingToken = await _dbContext.DeviceTokens
                    .FirstOrDefaultAsync(x => x.DeviceToken == deviceToken);

                if (existingToken != null)
                {
                    // Update existing token
                    existingToken.UserId = userId;
                    existingToken.DeviceType = deviceType;
                    existingToken.AppVersion = appVersion;
                    existingToken.IsActive = true;
                    existingToken.LastUsedAt = DateTime.UtcNow;
                    _dbContext.DeviceTokens.Update(existingToken);
                }
                else
                {
                    // Create new token
                    var newToken = new DeviceTokenEntity
                    {
                        UserId = userId,
                        DeviceToken = deviceToken,
                        DeviceType = deviceType,
                        AppVersion = appVersion,
                        IsActive = true,
                        RegisteredAt = DateTime.UtcNow
                    };
                    await _dbContext.DeviceTokens.AddAsync(newToken);
                }

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation($"Device token registered for user {userId}");
                return true;
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError($"Database error registering token: {ex.Message}", ex);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error registering device token: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<bool> UnregisterTokenAsync(string deviceToken)
        {
            try
            {
                if (string.IsNullOrEmpty(deviceToken))
                {
                    _logger.LogWarning("Invalid deviceToken for unregistration");
                    return false;
                }

                var token = await _dbContext.DeviceTokens
                    .FirstOrDefaultAsync(x => x.DeviceToken == deviceToken);

                if (token == null)
                {
                    _logger.LogWarning($"Token not found for unregistration: {deviceToken}");
                    return false;
                }

                _dbContext.DeviceTokens.Remove(token);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation($"Device token unregistered: {deviceToken}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error unregistering device token: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<List<DeviceTokenEntity>> GetActiveTokensForUserAsync(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    _logger.LogWarning("Invalid userId for retrieving tokens");
                    return new List<DeviceTokenEntity>();
                }

                var tokens = await _dbContext.DeviceTokens
                    .Where(x => x.UserId == userId && x.IsActive)
                    .ToListAsync();

                return tokens;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error retrieving active tokens for user {userId}: {ex.Message}", ex);
                return new List<DeviceTokenEntity>();
            }
        }

        public async Task<bool> DeactivateTokenAsync(string deviceToken)
        {
            try
            {
                if (string.IsNullOrEmpty(deviceToken))
                {
                    _logger.LogWarning("Invalid deviceToken for deactivation");
                    return false;
                }

                var token = await _dbContext.DeviceTokens
                    .FirstOrDefaultAsync(x => x.DeviceToken == deviceToken);

                if (token == null)
                {
                    _logger.LogWarning($"Token not found for deactivation: {deviceToken}");
                    return false;
                }

                token.IsActive = false;
                _dbContext.DeviceTokens.Update(token);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation($"Device token deactivated: {deviceToken}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error deactivating device token: {ex.Message}", ex);
                return false;
            }
        }

        public async Task<bool> UpdateLastUsedAsync(string deviceToken)
        {
            try
            {
                if (string.IsNullOrEmpty(deviceToken))
                {
                    return false;
                }

                var token = await _dbContext.DeviceTokens
                    .FirstOrDefaultAsync(x => x.DeviceToken == deviceToken);

                if (token == null)
                {
                    return false;
                }

                token.LastUsedAt = DateTime.UtcNow;
                _dbContext.DeviceTokens.Update(token);
                await _dbContext.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating last used time: {ex.Message}", ex);
                return false;
            }
        }

        public async Task CleanupInactiveTokensAsync(int inactiveDaysThreshold = 30)
        {
            try
            {
                var cutoffDate = DateTime.UtcNow.AddDays(-inactiveDaysThreshold);
                var inactiveTokens = await _dbContext.DeviceTokens
                    .Where(x => x.LastUsedAt == null || x.LastUsedAt < cutoffDate)
                    .ToListAsync();

                if (inactiveTokens.Count == 0)
                {
                    _logger.LogInformation("No inactive tokens to clean up");
                    return;
                }

                _dbContext.DeviceTokens.RemoveRange(inactiveTokens);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation($"Cleaned up {inactiveTokens.Count} inactive device tokens");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error cleaning up inactive tokens: {ex.Message}", ex);
            }
        }
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Notification.Application.Interfaces;
using Notification.Domain.Entities;
using Notification.Infrastructure.Data;

namespace Notification.Application.Services
{
    public class NotificationSettingsService : INotificationSettingsService
    {
        private readonly NotificationDbContext _dbContext;
        private readonly ILogger<NotificationSettingsService> _logger;

        public NotificationSettingsService(NotificationDbContext dbContext, ILogger<NotificationSettingsService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<NotificationSettingsEntity?> GetUserSettingsAsync(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return null;
                }

                var settings = await _dbContext.NotificationSettings
                    .FirstOrDefaultAsync(x => x.UserId == userId);

                return settings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving notification settings for user {UserId}", userId);
                return null;
            }
        }

        public async Task<NotificationSettingsEntity> CreateOrUpdateSettingsAsync(NotificationSettingsEntity settings)
        {
            try
            {
                if (string.IsNullOrEmpty(settings.UserId))
                {
                    throw new ArgumentException("UserId cannot be empty", nameof(settings));
                }

                var existingSettings = await _dbContext.NotificationSettings
                    .FirstOrDefaultAsync(x => x.UserId == settings.UserId);

                if (existingSettings == null)
                {
                    settings.UpdatedAt = DateTime.UtcNow;
                    await _dbContext.NotificationSettings.AddAsync(settings);
                }
                else
                {
                    existingSettings.PushNotificationsEnabled = settings.PushNotificationsEnabled;
                    existingSettings.SoundEnabled = settings.SoundEnabled;
                    existingSettings.VibrateEnabled = settings.VibrateEnabled;
                    existingSettings.ChatNotificationsEnabled = settings.ChatNotificationsEnabled;
                    existingSettings.MentionNotificationsEnabled = settings.MentionNotificationsEnabled;
                    existingSettings.UpdatedAt = DateTime.UtcNow;
                    _dbContext.NotificationSettings.Update(existingSettings);
                }

                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Notification settings updated for user {UserId}", settings.UserId);

                return existingSettings ?? settings;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating/updating notification settings for user {UserId}", settings.UserId);
                throw;
            }
        }

        public async Task<bool> DeleteSettingsAsync(string userId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                {
                    return false;
                }

                var settings = await _dbContext.NotificationSettings
                    .FirstOrDefaultAsync(x => x.UserId == userId);

                if (settings == null)
                {
                    return false;
                }

                _dbContext.NotificationSettings.Remove(settings);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation("Notification settings deleted for user {UserId}", userId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting notification settings for user {UserId}", userId);
                return false;
            }
        }
    }
}

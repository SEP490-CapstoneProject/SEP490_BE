using Notification.Domain.Entities;

namespace Notification.Application.Interfaces
{
    public interface INotificationSettingsService
    {
        Task<NotificationSettingsEntity?> GetUserSettingsAsync(string userId);
        Task<NotificationSettingsEntity> CreateOrUpdateSettingsAsync(NotificationSettingsEntity settings);
        Task<bool> DeleteSettingsAsync(string userId);
    }
}

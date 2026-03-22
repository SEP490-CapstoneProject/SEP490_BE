using Notification.Domain.Entities;

namespace Notification.Application.Interfaces;

public interface INotificationRepository
{
    Task<(List<NotificationEntity> Items, int? NextCursor)> GetNotificationsAsync(string userId, int? cursor, int limit);
    Task<int> GetUnreadCountAsync(string userId);
    Task<NotificationEntity> CreateAsync(NotificationEntity entity);
    Task MarkAsReadAsync(int id, string userId);
    Task MarkAllAsReadAsync(string userId);
}

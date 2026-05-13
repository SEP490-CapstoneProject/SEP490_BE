using Notification.Application.DTOs;
using Notification.Domain.Entities;

namespace Notification.Application.Interfaces;

public interface INotificationService
{
    Task<CursorPagedResult<UserNotificationDto>> GetNotificationsAsync(string userId, int? cursor, int limit);
    Task<CursorPagedResult<UserNotificationDto>> GetCommunityNotificationsAsync(string userId, int? cursor, int limit);
    Task<CursorPagedResult<UserNotificationDto>> GetSystemNotificationsAsync(string userId, int? cursor, int limit);
    Task<int> GetUnreadCountAsync(string userId);
    Task<UserNotificationDto> CreateNotificationAsync(NotificationEntity entity);
    Task<NotificationCreatedEventDto> BuildCreatedEventAsync(NotificationEntity entity);
    Task MarkAsReadAsync(int id, string userId);
    Task MarkAllAsReadAsync(string userId);
}

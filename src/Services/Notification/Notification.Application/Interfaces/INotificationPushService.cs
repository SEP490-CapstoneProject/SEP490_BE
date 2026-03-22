using Notification.Application.DTOs;

namespace Notification.Application.Interfaces;

public interface INotificationPushService
{
    Task PushAsync(string userId, UserNotificationDto dto);
}

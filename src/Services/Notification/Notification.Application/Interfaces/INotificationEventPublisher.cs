using Notification.Application.DTOs;

namespace Notification.Application.Interfaces;

public interface INotificationEventPublisher
{
    Task PublishNotificationCreatedAsync(NotificationCreatedEventDto evt, CancellationToken cancellationToken = default);
}

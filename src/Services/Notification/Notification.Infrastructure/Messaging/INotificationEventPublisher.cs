using Notification.Application.DTOs;

namespace Notification.Infrastructure.Messaging;

public interface INotificationEventPublisher
{
    Task PublishNotificationCreatedAsync(NotificationCreatedEventDto evt, CancellationToken cancellationToken = default);
}

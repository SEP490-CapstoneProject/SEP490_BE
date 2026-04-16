namespace Connection.Application.Interfaces;

/// <summary>
/// Publishes connection- and message-related events to RabbitMQ
/// so the Realtime Service can forward them via SignalR.
/// </summary>
public interface IConnectionEventPublisher
{
    Task PublishConnectionRequestedAsync(int connectionId, int fromUserId, int toUserId, int profileId, DateTime requestedAt, CancellationToken cancellationToken = default);
    Task PublishConnectionAcceptedAsync(int connectionId, int fromUserId, int toUserId, DateTime acceptedAt, CancellationToken cancellationToken = default);
    Task PublishNewMessageNotificationAsync(int messageId, int roomId, int fromUserId, int toUserId, string content, DateTime sentAt, CancellationToken cancellationToken = default);
}

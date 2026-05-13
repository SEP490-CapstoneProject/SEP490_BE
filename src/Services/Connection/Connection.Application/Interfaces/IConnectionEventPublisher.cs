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
    Task PublishNewMessageNotificationAsync(int messageId, int roomId, int fromUserId, int toUserId, string content, DateTime sentAt, Connection.Application.DTOs.NotificationActorDto? author, CancellationToken cancellationToken = default);
    Task PublishConnectionRequestNotificationAsync(ConnectionNotificationEventPayload payload, CancellationToken cancellationToken = default);
    Task PublishConnectionAcceptedNotificationAsync(ConnectionNotificationEventPayload payload, CancellationToken cancellationToken = default);
}

public class ConnectionNotificationEventPayload
{
    public string EventId { get; set; } = Guid.NewGuid().ToString("N");
    public string EventType { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public string UserId { get; set; } = string.Empty;
    public string? ActorId { get; set; }
    public string ActorType { get; set; } = "USER";
    public string? ObjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Connection.Application.DTOs.NotificationActorDto? Author { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

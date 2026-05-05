using System.Collections.Generic;
using System.Threading.Tasks;

namespace Realtime.Application.Clients;

/// <summary>
/// HTTP client to call Notification Service for FCM push notifications.
/// Used to send push notifications for chat messages when app is closed.
/// </summary>
public interface INotificationServiceClient
{
    /// <summary>
    /// Send FCM push notification for a new chat message.
    /// </summary>
    /// <param name="toUserId">Recipient user ID</param>
    /// <param name="messageId">Chat message ID</param>
    /// <param name="roomId">Chat room ID</param>
    /// <param name="senderName">Sender's display name</param>
    /// <param name="messagePreview">Message content preview (first 100 chars)</param>
    /// <returns>True if FCM send succeeded, false otherwise</returns>
    Task<bool> SendChatMessageNotificationAsync(
        int toUserId,
        int messageId,
        int roomId,
        string senderName,
        string messagePreview);

    /// <summary>
    /// Send FCM push notification for multiple new messages (aggregated).
    /// </summary>
    /// <param name="toUserId">Recipient user ID</param>
    /// <param name="totalMessageCount">Total number of new messages</param>
    /// <returns>True if FCM send succeeded, false otherwise</returns>
    Task<bool> SendAggregatedMessageNotificationAsync(int toUserId, int totalMessageCount);
}

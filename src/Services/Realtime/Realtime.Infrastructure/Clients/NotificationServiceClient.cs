using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Realtime.Application.Clients;

namespace Realtime.Infrastructure.Clients;

/// <summary>
/// HTTP client implementation for calling Notification Service.
/// Handles FCM push notifications for chat messages.
/// </summary>
public class NotificationServiceClient : INotificationServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NotificationServiceClient> _logger;

    public NotificationServiceClient(HttpClient httpClient, ILogger<NotificationServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Send FCM push notification for a new chat message.
    /// </summary>
    public async Task<bool> SendChatMessageNotificationAsync(
        int toUserId,
        int messageId,
        int roomId,
        string senderName,
        string messagePreview)
    {
        try
        {
            var payload = new
            {
                toUserId,
                messageId,
                roomId,
                senderName,
                messagePreview = messagePreview?.Substring(0, Math.Min(100, messagePreview?.Length ?? 0)) ?? "New message"
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                "/api/fcm/send-message-notification",
                content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to send FCM message notification to user {UserId}. Status: {StatusCode}",
                    toUserId,
                    response.StatusCode);
                return false;
            }

            _logger.LogInformation(
                "FCM message notification sent to user {UserId} for message {MessageId} in room {RoomId}",
                toUserId,
                messageId,
                roomId);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error sending FCM message notification to user {UserId}",
                toUserId);
            return false;
        }
    }

    /// <summary>
    /// Send FCM push notification for multiple new messages (aggregated).
    /// </summary>
    public async Task<bool> SendAggregatedMessageNotificationAsync(int toUserId, int totalMessageCount)
    {
        try
        {
            var payload = new
            {
                toUserId,
                totalMessageCount
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync(
                "/api/fcm/send-aggregated-notification",
                content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Failed to send FCM aggregated notification to user {UserId}. Status: {StatusCode}",
                    toUserId,
                    response.StatusCode);
                return false;
            }

            _logger.LogInformation(
                "FCM aggregated notification sent to user {UserId} for {MessageCount} messages",
                toUserId,
                totalMessageCount);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error sending FCM aggregated notification to user {UserId}",
                toUserId);
            return false;
        }
    }
}

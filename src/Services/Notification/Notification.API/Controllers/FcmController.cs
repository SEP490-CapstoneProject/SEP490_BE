using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notification.Application.Interfaces;

namespace Notification.API.Controllers;

/// <summary>
/// Internal FCM notification endpoints.
/// Called by Realtime service to send push notifications for offline delivery.
/// These endpoints should only be called from authenticated internal services.
/// </summary>
[ApiController]
[Route("api/fcm")]
[AllowAnonymous]
public class FcmController : ControllerBase
{
    private readonly IFcmService _fcmService;
    private readonly IDeviceTokenService _deviceTokenService;
    private readonly ILogger<FcmController> _logger;

    public FcmController(
        IFcmService fcmService,
        IDeviceTokenService deviceTokenService,
        ILogger<FcmController> logger)
    {
        _fcmService = fcmService;
        _deviceTokenService = deviceTokenService;
        _logger = logger;
    }

    /// <summary>
    /// Send FCM push notification for a new chat message.
    /// Called by Realtime service when a message is sent.
    /// </summary>
    [HttpPost("send-message-notification")]
    public async Task<IActionResult> SendMessageNotification(
        [FromBody] SendMessageNotificationRequest? request)
    {
        try
        {
            if (request == null)
                return BadRequest("Request body is required");

            if (request.ToUserId <= 0)
                return BadRequest("Invalid toUserId");

            if (string.IsNullOrEmpty(request.SenderName))
                return BadRequest("SenderName is required");

            // Get device tokens for recipient
            var deviceTokenEntities = await _deviceTokenService.GetActiveTokensForUserAsync(request.ToUserId.ToString());
            var tokens = deviceTokenEntities.Select(dt => dt.DeviceToken).ToList();

            if (!tokens.Any())
            {
                _logger.LogInformation(
                    "No active device tokens for user {UserId}, skipping FCM notification",
                    request.ToUserId);
                return Ok(new { success = true, message = "No active tokens" });
            }

            // Build notification
            var title = $"Message from {request.SenderName}";
            var body = request.MessagePreview ?? "New message";

            var data = new Dictionary<string, string>
            {
                { "messageId", request.MessageId?.ToString() ?? "0" },
                { "roomId", request.RoomId?.ToString() ?? "0" },
                { "type", "chat_message" },
                { "deepLink", $"app://chat/{request.RoomId}" }
            };

            // Send FCM notification
            await _fcmService.SendMulticastAsync(tokens, title, body, data);

            _logger.LogInformation(
                "FCM message notification sent to user {UserId}. Tokens: {TokenCount}, Message: {MessageId}",
                request.ToUserId,
                tokens.Count,
                request.MessageId);

            return Ok(new { success = true, message = "Notification sent" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending FCM message notification");
            return StatusCode(500, new { success = false, message = "Error sending notification" });
        }
    }

    /// <summary>
    /// Send aggregated FCM push notification for multiple messages.
    /// Called by Realtime service after debouncing messages for a user.
    /// </summary>
    [HttpPost("send-aggregated-notification")]
    public async Task<IActionResult> SendAggregatedNotification(
        [FromBody] SendAggregatedNotificationRequest? request)
    {
        try
        {
            if (request == null)
                return BadRequest("Request body is required");

            if (request.ToUserId <= 0)
                return BadRequest("Invalid toUserId");

            if (request.TotalMessageCount <= 0)
                return BadRequest("TotalMessageCount must be > 0");

            // Get device tokens for recipient
            var deviceTokenEntities = await _deviceTokenService.GetActiveTokensForUserAsync(request.ToUserId.ToString());
            var tokens = deviceTokenEntities.Select(dt => dt.DeviceToken).ToList();

            if (!tokens.Any())
            {
                _logger.LogInformation(
                    "No active device tokens for user {UserId}, skipping FCM notification",
                    request.ToUserId);
                return Ok(new { success = true, message = "No active tokens" });
            }

            // Build notification
            var title = "New Messages";
            var body = request.TotalMessageCount == 1
                ? "You have 1 new message"
                : $"You have {request.TotalMessageCount} new messages";

            var data = new Dictionary<string, string>
            {
                { "type", "aggregated_messages" },
                { "messageCount", request.TotalMessageCount.ToString() },
                { "deepLink", "app://chat" }
            };

            // Send FCM notification
            await _fcmService.SendMulticastAsync(tokens, title, body, data);

            _logger.LogInformation(
                "FCM aggregated notification sent to user {UserId}. Tokens: {TokenCount}, Messages: {MessageCount}",
                request.ToUserId,
                tokens.Count,
                request.TotalMessageCount);

            return Ok(new { success = true, message = "Notification sent" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending FCM aggregated notification");
            return StatusCode(500, new { success = false, message = "Error sending notification" });
        }
    }
}

public class SendMessageNotificationRequest
{
    public int ToUserId { get; set; }
    public int? MessageId { get; set; }
    public int? RoomId { get; set; }
    public string SenderName { get; set; } = "";
    public string? MessagePreview { get; set; }
}

public class SendAggregatedNotificationRequest
{
    public int ToUserId { get; set; }
    public int TotalMessageCount { get; set; }
}

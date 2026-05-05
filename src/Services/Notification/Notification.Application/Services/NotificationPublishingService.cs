using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Notification.Application.DTOs;
using Notification.Application.Interfaces;
using Notification.Domain.Entities;

namespace Notification.Application.Services
{
    public interface INotificationPublishingService
    {
        /// <summary>Send notification via both realtime (SignalR) and FCM channels</summary>
        Task SendDualChannelAsync(NotificationEntity notification, NotificationCreatedEventDto createdEvent);

        /// <summary>Send notification via realtime (SignalR) channel only</summary>
        Task SendRealtimeOnlyAsync(NotificationCreatedEventDto createdEvent);

        /// <summary>Send notification via FCM push channel only</summary>
        Task SendFcmOnlyAsync(NotificationEntity notification);
    }

    public class NotificationPublishingService : INotificationPublishingService
    {
        private readonly INotificationEventPublisher _eventPublisher;
        private readonly IFcmService _fcmService;
        private readonly IDeviceTokenService _deviceTokenService;
        private readonly INotificationSettingsService _settingsService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<NotificationPublishingService> _logger;

        public NotificationPublishingService(
            INotificationEventPublisher eventPublisher,
            IFcmService? fcmService,
            IDeviceTokenService? deviceTokenService,
            INotificationSettingsService? settingsService,
            IConfiguration configuration,
            ILogger<NotificationPublishingService> logger)
        {
            _eventPublisher = eventPublisher;
            _fcmService = fcmService;
            _deviceTokenService = deviceTokenService;
            _settingsService = settingsService;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendDualChannelAsync(NotificationEntity notification, NotificationCreatedEventDto createdEvent)
        {
            // Send via FCM first (fire and forget)
            _ = SendFcmOnlyAsync(notification).ContinueWith(task =>
            {
                if (task.IsFaulted)
                {
                    _logger.LogError(task.Exception, "Error sending FCM notification");
                }
            });

            // Send via realtime (primary channel)
            await SendRealtimeOnlyAsync(createdEvent);
        }

        public async Task SendRealtimeOnlyAsync(NotificationCreatedEventDto createdEvent)
        {
            try
            {
                var fcmEnabled = _configuration.GetValue<bool>("FCM:Enabled", true);
                if (!fcmEnabled)
                {
                    _logger.LogDebug("FCM disabled in configuration");
                    return;
                }

                await _eventPublisher.PublishNotificationCreatedAsync(createdEvent);
                _logger.LogInformation("Notification published via realtime channel. NotificationId={NotificationId}", createdEvent.NotificationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing notification via realtime channel");
                throw;
            }
        }

        public async Task SendFcmOnlyAsync(NotificationEntity notification)
        {
            try
            {
                // Check if FCM is enabled
                var fcmEnabled = _configuration.GetValue<bool>("FCM:Enabled", true);
                if (!fcmEnabled)
                {
                    _logger.LogDebug("FCM disabled in configuration");
                    return;
                }

                if (_fcmService == null || _deviceTokenService == null)
                {
                    _logger.LogDebug("FCM or DeviceTokenService not available");
                    return;
                }

                // Check user's notification settings
                if (_settingsService != null)
                {
                    var userSettings = await _settingsService.GetUserSettingsAsync(notification.UserId);
                    if (userSettings != null && !userSettings.PushNotificationsEnabled)
                    {
                        _logger.LogDebug("Push notifications disabled for user {UserId}", notification.UserId);
                        return;
                    }
                }

                // Get active device tokens for user
                var deviceTokens = await _deviceTokenService.GetActiveTokensForUserAsync(notification.UserId);

                if (deviceTokens == null || deviceTokens.Count == 0)
                {
                    _logger.LogDebug("No active device tokens for user {UserId}", notification.UserId);
                    return;
                }

                // Convert notification to FCM format
                var data = new Dictionary<string, string>
                {
                    { "notificationId", notification.Id.ToString() },
                    { "notificationType", notification.Type },
                    { "deepLink", $"app://notification/{notification.Id}" }
                };

                if (!string.IsNullOrEmpty(notification.ActorId))
                    data["actorId"] = notification.ActorId;

                if (!string.IsNullOrEmpty(notification.ObjectId))
                    data["objectId"] = notification.ObjectId;

                if (!string.IsNullOrEmpty(notification.EventId))
                    data["eventId"] = notification.EventId;

                // Send to all devices
                if (deviceTokens.Count == 1)
                {
                    var messageId = await _fcmService.SendNotificationAsync(
                        deviceTokens[0].DeviceToken,
                        notification.Title,
                        notification.Content,
                        data);

                    if (!string.IsNullOrEmpty(messageId))
                    {
                        _logger.LogInformation(
                            "FCM notification sent. NotificationId={NotificationId}, UserId={UserId}, MessageId={MessageId}",
                            notification.Id, notification.UserId, messageId);
                    }
                }
                else
                {
                    var tokens = deviceTokens.Select(dt => dt.DeviceToken).ToList();
                    var success = await _fcmService.SendMulticastAsync(tokens, notification.Title, notification.Content, data);

                    if (success)
                    {
                        _logger.LogInformation(
                            "FCM multicast sent. NotificationId={NotificationId}, UserId={UserId}, DeviceCount={DeviceCount}",
                            notification.Id, notification.UserId, deviceTokens.Count);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending FCM notification. NotificationId={NotificationId}", notification.Id);
            }
        }
    }
}

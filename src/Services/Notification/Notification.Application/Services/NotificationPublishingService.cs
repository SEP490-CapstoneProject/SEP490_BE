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
            _logger.LogInformation("📱 [FCM_ATTEMPT] NotificationId={NotificationId}, UserId={UserId}, Type={Type}",
                notification.Id, notification.UserId, notification.Type);

            try
            {
                var fcmEnabled = _configuration.GetValue<bool>("FCM:Enabled", true);
                if (!fcmEnabled)
                {
                    _logger.LogInformation("📱 [FCM_DISABLED_CONFIG] FCM disabled in configuration for NotificationId={NotificationId}",
                        notification.Id);
                    return;
                }

                if (_fcmService == null || _deviceTokenService == null)
                {
                    _logger.LogWarning("📱 [FCM_UNAVAILABLE] FCMService={Available}, DeviceTokenService={Available} for NotificationId={NotificationId}",
                        _fcmService != null, _deviceTokenService != null, notification.Id);
                    return;
                }

                if (_settingsService != null)
                {
                    var userSettings = await _settingsService.GetUserSettingsAsync(notification.UserId);
                    if (userSettings != null && !userSettings.PushNotificationsEnabled)
                    {
                        _logger.LogInformation("📱 [FCM_DISABLED_USER] Push notifications disabled by user {UserId} for NotificationId={NotificationId}",
                            notification.UserId, notification.Id);
                        return;
                    }
                }

                var deviceTokens = await _deviceTokenService.GetActiveTokensForUserAsync(notification.UserId);
                var tokenCount = deviceTokens?.Count ?? 0;
                var deviceTypes = deviceTokens != null ? string.Join(",", deviceTokens.Select(dt => dt.DeviceType).Distinct()) : "none";
                
                _logger.LogInformation("📱 [FCM_TOKENS] UserId={UserId}, TokenCount={TokenCount}, DeviceTypes={DeviceTypes}, NotificationId={NotificationId}",
                    notification.UserId, tokenCount, deviceTypes, notification.Id);

                if (deviceTokens == null || deviceTokens.Count == 0)
                {
                    _logger.LogWarning("📱 [FCM_NO_TOKENS] No active device tokens for user {UserId}, NotificationId={NotificationId}",
                        notification.UserId, notification.Id);
                    return;
                }

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

                _logger.LogDebug("📱 [FCM_PAYLOAD] Title={Title}, BodyLength={BodyLength}, DataKeys={DataKeys}, NotificationId={NotificationId}",
                    notification.Title, notification.Content?.Length ?? 0, string.Join(",", data.Keys), notification.Id);

                if (deviceTokens.Count == 1)
                {
                    _logger.LogInformation("📱 [FCM_SINGLE_SEND] Sending to 1 device. DeviceType={DeviceType}, NotificationId={NotificationId}",
                        deviceTokens[0].DeviceType, notification.Id);

                    var messageId = await _fcmService.SendNotificationAsync(
                        deviceTokens[0].DeviceToken,
                        notification.Title,
                        notification.Content,
                        data);

                    if (!string.IsNullOrEmpty(messageId))
                    {
                        _logger.LogInformation("📱 [FCM_SEND_SUCCESS] Single send succeeded. NotificationId={NotificationId}, UserId={UserId}, MessageId={MessageId}",
                            notification.Id, notification.UserId, messageId);
                    }
                    else
                    {
                        _logger.LogWarning("📱 [FCM_SEND_FAILED] Single send returned null MessageId for NotificationId={NotificationId}",
                            notification.Id);
                    }
                }
                else
                {
                    var deviceTypesList = string.Join(",", deviceTokens.Select(dt => dt.DeviceType));
                    _logger.LogInformation("📱 [FCM_MULTICAST_SEND] Sending to {Count} devices. DeviceTypes={DeviceTypes}, NotificationId={NotificationId}",
                        deviceTokens.Count, deviceTypesList, notification.Id);

                    var tokens = deviceTokens.Select(dt => dt.DeviceToken).ToList();
                    var success = await _fcmService.SendMulticastAsync(tokens, notification.Title, notification.Content, data);

                    if (success)
                    {
                        _logger.LogInformation("📱 [FCM_MULTICAST_SUCCESS] Multicast succeeded. NotificationId={NotificationId}, UserId={UserId}, DeviceCount={DeviceCount}",
                            notification.Id, notification.UserId, deviceTokens.Count);
                    }
                    else
                    {
                        _logger.LogWarning("📱 [FCM_MULTICAST_FAILED] Multicast had failures. NotificationId={NotificationId}, DeviceCount={DeviceCount}",
                            notification.Id, deviceTokens.Count);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "📱 [FCM_ERROR] Exception sending FCM notification. NotificationId={NotificationId}, UserId={UserId}",
                    notification.Id, notification.UserId);
            }
        }
    }
}

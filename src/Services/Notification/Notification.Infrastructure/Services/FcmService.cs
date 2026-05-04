using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Notification.Infrastructure.Services
{
    public interface IFcmService
    {
        Task<string?> SendNotificationAsync(string deviceToken, string title, string body, Dictionary<string, string>? data = null);
        Task<List<SendResponse>?> SendMulticastAsync(List<string> deviceTokens, string title, string body, Dictionary<string, string>? data = null);
        Task<bool> ValidateTokenAsync(string deviceToken);
    }

    public class FcmService : IFcmService
    {
        private readonly ILogger<FcmService> _logger;
        private readonly IConfiguration _configuration;

        public FcmService(ILogger<FcmService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public async Task<string?> SendNotificationAsync(string deviceToken, string title, string body, Dictionary<string, string>? data = null)
        {
            try
            {
                if (string.IsNullOrEmpty(deviceToken))
                {
                    _logger.LogWarning("Device token is empty");
                    return null;
                }

                var message = new Message
                {
                    Token = deviceToken,
                    Notification = new FirebaseAdmin.Messaging.Notification
                    {
                        Title = title,
                        Body = body
                    },
                    Data = data ?? new Dictionary<string, string>(),
                    Android = new AndroidConfig
                    {
                        Priority = Priority.High
                    },
                    Apns = new ApnsConfig
                    {
                        Headers = new Dictionary<string, string>
                        {
                            { "apns-priority", "10" } // High priority for iOS
                        }
                    }
                };

                string messageId = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                _logger.LogInformation($"FCM message sent successfully. MessageId: {messageId}, Token: {deviceToken}");
                return messageId;
            }
            catch (FirebaseMessagingException ex)
            {
                _logger.LogError($"FCM error sending to token {deviceToken}: {ex.Message}", ex);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error sending FCM notification: {ex.Message}", ex);
                return null;
            }
        }

        public async Task<List<SendResponse>?> SendMulticastAsync(List<string> deviceTokens, string title, string body, Dictionary<string, string>? data = null)
        {
            try
            {
                if (deviceTokens == null || deviceTokens.Count == 0)
                {
                    _logger.LogWarning("Device tokens list is empty");
                    return new List<SendResponse>();
                }

                var message = new MulticastMessage
                {
                    Tokens = deviceTokens,
                    Notification = new FirebaseAdmin.Messaging.Notification
                    {
                        Title = title,
                        Body = body
                    },
                    Data = data ?? new Dictionary<string, string>(),
                    Android = new AndroidConfig
                    {
                        Priority = Priority.High
                    },
                    Apns = new ApnsConfig
                    {
                        Headers = new Dictionary<string, string>
                        {
                            { "apns-priority", "10" }
                        }
                    }
                };

                var response = await FirebaseMessaging.DefaultInstance.SendMulticastAsync(message);
                _logger.LogInformation($"FCM multicast sent. Success: {response.SuccessCount}, Failed: {response.FailureCount}");
                return new List<SendResponse>(response.Responses);
            }
            catch (FirebaseMessagingException ex)
            {
                _logger.LogError($"FCM error sending multicast: {ex.Message}", ex);
                return new List<SendResponse>();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error sending multicast: {ex.Message}", ex);
                return new List<SendResponse>();
            }
        }

        public async Task<bool> ValidateTokenAsync(string deviceToken)
        {
            try
            {
                if (string.IsNullOrEmpty(deviceToken))
                {
                    return false;
                }

                // Note: Firebase doesn't have a direct validation method
                // This is a placeholder - in production, you'd validate during send and handle errors
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error validating token: {ex.Message}", ex);
                return false;
            }
        }
    }
}


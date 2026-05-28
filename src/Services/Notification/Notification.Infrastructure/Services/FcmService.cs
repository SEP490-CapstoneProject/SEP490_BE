using FirebaseAdmin.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Notification.Application.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Notification.Infrastructure.Services
{
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
            if (string.IsNullOrEmpty(deviceToken))
            {
                _logger.LogWarning("📱 [FCM_EMPTY_TOKEN] Attempting to send FCM with empty device token");
                return null;
            }

            _logger.LogInformation("📱 [FCM_SEND_START] Title={Title}, BodyLength={BodyLength}, DataKeys={DataKeys}, Token={Token}",
                title, body?.Length ?? 0, data != null ? string.Join(",", data.Keys) : "none", deviceToken);

            try
            {
                if (FirebaseMessaging.DefaultInstance == null)
                {
                    _logger.LogError("📱 [FCM_FIREBASE_UNAVAILABLE] FirebaseMessaging.DefaultInstance is null - Firebase Admin SDK not initialized");
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
                            { "apns-priority", "10" }
                        }
                    }
                };

                string messageId = await FirebaseMessaging.DefaultInstance.SendAsync(message);
                _logger.LogInformation("📱 [FCM_SEND_SUCCESS] FCM message sent. MessageId={MessageId}, Token={Token}, Title={Title}",
                    messageId, deviceToken, title);
                return messageId;
            }
            catch (FirebaseMessagingException ex)
            {
                _logger.LogError("📱 [FCM_SEND_FAILED] Firebase messaging error. ErrorCode={ErrorCode}, Message={Message}, Token={Token}",
                    ex.ErrorCode, ex.Message, deviceToken);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "📱 [FCM_SEND_ERROR] Unexpected error sending FCM. Token={Token}",
                    deviceToken);
                return null;
            }
        }

        public async Task<bool> SendMulticastAsync(List<string> deviceTokens, string title, string body, Dictionary<string, string>? data = null)
        {
            if (deviceTokens == null || deviceTokens.Count == 0)
            {
                _logger.LogWarning("📱 [FCM_MULTICAST_EMPTY] No device tokens provided for multicast send");
                return false;
            }

            _logger.LogInformation("📱 [FCM_MULTICAST_START] Count={Count}, Title={Title}, BodyLength={BodyLength}, DataKeys={DataKeys}",
                deviceTokens.Count, title, body?.Length ?? 0, data != null ? string.Join(",", data.Keys) : "none");
            
            // Log each token (first 50 chars) for debugging
            for (int i = 0; i < deviceTokens.Count; i++)
            {
                var token = deviceTokens[i];
                if (!string.IsNullOrEmpty(token))
                {
                    var tokenPreview = token.Substring(0, Math.Min(50, token.Length)) + "...";
                    _logger.LogInformation("📱 [FCM_MULTICAST_TOKEN] Index={Index}, TokenPreview={TokenPreview}, Length={Length}",
                        i, tokenPreview, token.Length);
                }
            }

            try
            {
                if (FirebaseMessaging.DefaultInstance == null)
                {
                    _logger.LogError("📱 [FCM_FIREBASE_UNAVAILABLE] FirebaseMessaging.DefaultInstance is null - Firebase Admin SDK not initialized");
                    return false;
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
                
                if (response == null)
                {
                    _logger.LogWarning("📱 [FCM_MULTICAST_NULL_RESPONSE] SendMulticastAsync returned null for {Count} devices", deviceTokens.Count);
                    return false;
                }
                
                _logger.LogInformation("📱 [FCM_MULTICAST_RESPONSE] Success={SuccessCount}, Failed={FailureCount}, Total={Total}",
                    response.SuccessCount, response.FailureCount, deviceTokens.Count);

                if (response.FailureCount > 0 && response.Responses != null && response.Responses.Count > 0)
                {
                    var failedCount = 0;
                    var errorDetails = new List<string>();
                    
                    for (int i = 0; i < response.Responses.Count; i++)
                    {
                        var sendResponse = response.Responses[i];
                        if (!sendResponse.IsSuccess)
                        {
                            failedCount++;
                            var tokenPreview = i < deviceTokens.Count ? deviceTokens[i].Substring(0, Math.Min(30, deviceTokens[i].Length)) + "..." : "unknown";
                            
                            if (sendResponse.Exception != null)
                            {
                                var ex = sendResponse.Exception;
                                if (ex is FirebaseMessagingException fmEx)
                                {
                                    _logger.LogWarning("📱 [FCM_MULTICAST_ERROR_DETAIL] Index={Index}, Token={Token}, ErrorCode={ErrorCode}, Message={Message}",
                                        i, tokenPreview, fmEx.ErrorCode, fmEx.Message);
                                    errorDetails.Add($"Index={i}, ErrorCode={fmEx.ErrorCode}, Message={fmEx.Message}");
                                }
                                else
                                {
                                    _logger.LogWarning("📱 [FCM_MULTICAST_ERROR_DETAIL] Index={Index}, Token={Token}, ExceptionType={ExceptionType}, Message={Message}",
                                        i, tokenPreview, ex.GetType().Name, ex.Message);
                                    errorDetails.Add($"Index={i}, Type={ex.GetType().Name}, Message={ex.Message}");
                                }
                            }
                            
                            if (errorDetails.Count >= 5)
                                break;
                        }
                    }

                    if (errorDetails.Count > 0)
                    {
                        _logger.LogWarning("📱 [FCM_MULTICAST_ERRORS] SampleErrors: {ErrorDetails}",
                            string.Join(" | ", errorDetails));
                    }
                }

                return response.SuccessCount > 0;
            }
            catch (FirebaseMessagingException ex)
            {
                _logger.LogError("📱 [FCM_MULTICAST_FAILED] Firebase messaging error. ErrorCode={ErrorCode}, Message={Message}, TokenCount={TokenCount}",
                    ex.ErrorCode, ex.Message, deviceTokens.Count);
                return false;
            }
            catch (Exception ex) when (ex.GetType().Name.Contains("OAuth2") || ex.GetType().Name.Contains("TokenResponse"))
            {
                _logger.LogError(ex, "📱 [FCM_OAUTH2_ERROR] Google OAuth2/Token error. ExceptionType={ExceptionType}, Message={Message}, TokenCount={TokenCount}",
                    ex.GetType().Name, ex.Message, deviceTokens.Count);
                _logger.LogError("   Likely cause: Firebase credentials are invalid, expired, or missing in KeyVault");
                return false;
            }
            catch (Exception ex) when (ex.GetType().Name.Contains("Credential") || ex.GetType().Name.Contains("ServiceAccount"))
            {
                _logger.LogError(ex, "📱 [FCM_CREDENTIAL_ERROR] Firebase service account credential error. ExceptionType={ExceptionType}, Message={Message}, TokenCount={TokenCount}",
                    ex.GetType().Name, ex.Message, deviceTokens.Count);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "📱 [FCM_MULTICAST_ERROR] Unexpected error in multicast send. ExceptionType={ExceptionType}, TokenCount={TokenCount}",
                    ex.GetType().Name, deviceTokens.Count);
                return false;
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


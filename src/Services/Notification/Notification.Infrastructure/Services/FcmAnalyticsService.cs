using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Notification.Domain.Entities;
using Notification.Infrastructure.Data;

namespace Notification.Infrastructure.Services
{
    public interface IFcmAnalyticsService
    {
        Task<FcmAnalyticsMetrics> GetMetricsAsync(DateTime? since = null);
        Task<TokenHealthMetrics> GetTokenHealthAsync();
        Task<DeliveryMetrics> GetDeliveryMetricsAsync(DateTime? since = null);
        Task RecordDeliveryAttemptAsync(int deviceTokenId, int notificationId, string? messageId, string? errorCode, string? errorMessage);
    }

    public class FcmAnalyticsMetrics
    {
        public int TotalSent { get; set; }
        public int TotalSuccessful { get; set; }
        public int TotalFailed { get; set; }
        public double SuccessRate { get; set; }
        public Dictionary<string, int> ErrorBreakdown { get; set; } = new();
        public double AverageLatencyMs { get; set; }
        public DateTime CalculatedAt { get; set; }
    }

    public class TokenHealthMetrics
    {
        public int TotalTokens { get; set; }
        public int ActiveTokens { get; set; }
        public int InactiveTokens { get; set; }
        public double TokenValidityPercentage { get; set; }
        public int TokensByDeviceType { get; set; }
        public DateTime CalculatedAt { get; set; }
    }

    public class DeliveryMetrics
    {
        public int SentToday { get; set; }
        public int SuccessfulToday { get; set; }
        public int FailedToday { get; set; }
        public double SuccessRateToday { get; set; }
        public Dictionary<string, int> TopErrorsToday { get; set; } = new();
    }

    public class FcmAnalyticsService : IFcmAnalyticsService
    {
        private readonly NotificationDbContext _dbContext;
        private readonly ILogger<FcmAnalyticsService> _logger;

        public FcmAnalyticsService(NotificationDbContext dbContext, ILogger<FcmAnalyticsService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<FcmAnalyticsMetrics> GetMetricsAsync(DateTime? since = null)
        {
            try
            {
                var sinceDate = since ?? DateTime.UtcNow.AddDays(-7);

                var logs = await _dbContext.PushNotificationLogs
                    .Where(x => x.SentAt >= sinceDate)
                    .ToListAsync();

                if (logs.Count == 0)
                {
                    return new FcmAnalyticsMetrics
                    {
                        TotalSent = 0,
                        TotalSuccessful = 0,
                        TotalFailed = 0,
                        SuccessRate = 0,
                        CalculatedAt = DateTime.UtcNow
                    };
                }

                var successful = logs.Count(x => x.Status == "Sent");
                var failed = logs.Count(x => x.Status == "Failed" || x.Status == "Bounced");

                var errorBreakdown = logs
                    .Where(x => !string.IsNullOrEmpty(x.ErrorCode))
                    .GroupBy(x => x.ErrorCode)
                    .ToDictionary(x => x.Key, x => x.Count());

                var latencies = logs
                    .Select(x => 0.0) // Placeholder - latency calculation would need CreatedAt timestamp
                    .ToList();

                return new FcmAnalyticsMetrics
                {
                    TotalSent = logs.Count,
                    TotalSuccessful = successful,
                    TotalFailed = failed,
                    SuccessRate = successful > 0 ? (double)successful / logs.Count * 100 : 0,
                    ErrorBreakdown = errorBreakdown,
                    AverageLatencyMs = latencies.Any() ? latencies.Average() : 0,
                    CalculatedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating FCM analytics metrics");
                throw;
            }
        }

        public async Task<TokenHealthMetrics> GetTokenHealthAsync()
        {
            try
            {
                var tokens = await _dbContext.DeviceTokens.ToListAsync();
                var totalTokens = tokens.Count;
                var activeTokens = tokens.Count(x => x.IsActive);
                var inactiveTokens = totalTokens - activeTokens;

                var deviceTypeCount = tokens
                    .GroupBy(x => x.DeviceType)
                    .Count();

                return new TokenHealthMetrics
                {
                    TotalTokens = totalTokens,
                    ActiveTokens = activeTokens,
                    InactiveTokens = inactiveTokens,
                    TokenValidityPercentage = totalTokens > 0 ? (double)activeTokens / totalTokens * 100 : 0,
                    TokensByDeviceType = deviceTypeCount,
                    CalculatedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating token health metrics");
                throw;
            }
        }

        public async Task<DeliveryMetrics> GetDeliveryMetricsAsync(DateTime? since = null)
        {
            try
            {
                var sinceDate = since ?? DateTime.UtcNow.AddDays(-1);

                var logs = await _dbContext.PushNotificationLogs
                    .Where(x => x.SentAt >= sinceDate)
                    .ToListAsync();

                var successful = logs.Count(x => x.Status == "Sent");
                var failed = logs.Count(x => x.Status == "Failed" || x.Status == "Bounced");

                var topErrors = logs
                    .Where(x => !string.IsNullOrEmpty(x.ErrorCode))
                    .GroupBy(x => x.ErrorCode)
                    .OrderByDescending(x => x.Count())
                    .Take(5)
                    .ToDictionary(x => x.Key, x => x.Count());

                return new DeliveryMetrics
                {
                    SentToday = logs.Count,
                    SuccessfulToday = successful,
                    FailedToday = failed,
                    SuccessRateToday = logs.Count > 0 ? (double)successful / logs.Count * 100 : 0,
                    TopErrorsToday = topErrors
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calculating delivery metrics");
                throw;
            }
        }

        public async Task RecordDeliveryAttemptAsync(
            int deviceTokenId,
            int notificationId,
            string? messageId,
            string? errorCode,
            string? errorMessage)
        {
            try
            {
                var log = new PushNotificationLogEntity
                {
                    DeviceTokenId = deviceTokenId,
                    NotificationId = notificationId,
                    MessageId = messageId,
                    Status = string.IsNullOrEmpty(errorCode) ? "Sent" : "Failed",
                    ErrorCode = errorCode,
                    ErrorMessage = errorMessage,
                    SentAt = DateTime.UtcNow
                };

                _dbContext.PushNotificationLogs.Add(log);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation(
                    "Recorded delivery attempt - Notification: {NotificationId}, Device: {DeviceTokenId}, Status: {Status}",
                    notificationId, deviceTokenId, log.Status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recording delivery attempt");
                throw;
            }
        }
    }
}

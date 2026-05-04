using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Notification.Infrastructure.Services
{
    public interface IFcmRetryService
    {
        Task<T?> ExecuteWithRetryAsync<T>(
            Func<Task<T?>> operation,
            string operationName,
            int maxRetries = 3,
            int initialDelayMs = 1000);

        Task ExecuteWithRetryAsync(
            Func<Task> operation,
            string operationName,
            int maxRetries = 3,
            int initialDelayMs = 1000);

        bool IsRetryableError(Exception ex);
        void LogRetryAttempt(string operationName, int attempt, int maxRetries, Exception ex);
    }

    public class FcmRetryService : IFcmRetryService
    {
        private readonly ILogger<FcmRetryService> _logger;
        private const int MaxRetries = 3;
        private const int InitialDelayMs = 1000;
        private const double ExponentialBase = 2.0;

        public FcmRetryService(ILogger<FcmRetryService> logger)
        {
            _logger = logger;
        }

        public async Task<T?> ExecuteWithRetryAsync<T>(
            Func<Task<T?>> operation,
            string operationName,
            int maxRetries = MaxRetries,
            int initialDelayMs = InitialDelayMs)
        {
            ArgumentNullException.ThrowIfNull(operation);

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    return await operation();
                }
                catch (Exception ex) when (IsRetryableError(ex) && attempt < maxRetries)
                {
                    LogRetryAttempt(operationName, attempt, maxRetries, ex);
                    
                    var delayMs = (int)(initialDelayMs * Math.Pow(ExponentialBase, attempt - 1));
                    await Task.Delay(delayMs);
                }
                catch (Exception ex) when (!IsRetryableError(ex))
                {
                    _logger.LogError(ex, 
                        "Non-retryable error in operation {OperationName}", operationName);
                    throw;
                }
            }

            _logger.LogError(
                "Operation {OperationName} failed after {MaxRetries} retries", 
                operationName, maxRetries);
            return default;
        }

        public async Task ExecuteWithRetryAsync(
            Func<Task> operation,
            string operationName,
            int maxRetries = MaxRetries,
            int initialDelayMs = InitialDelayMs)
        {
            ArgumentNullException.ThrowIfNull(operation);

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    await operation();
                    return;
                }
                catch (Exception ex) when (IsRetryableError(ex) && attempt < maxRetries)
                {
                    LogRetryAttempt(operationName, attempt, maxRetries, ex);
                    
                    var delayMs = (int)(initialDelayMs * Math.Pow(ExponentialBase, attempt - 1));
                    await Task.Delay(delayMs);
                }
                catch (Exception ex) when (!IsRetryableError(ex))
                {
                    _logger.LogError(ex, 
                        "Non-retryable error in operation {OperationName}", operationName);
                    throw;
                }
            }

            throw new InvalidOperationException(
                $"Operation {operationName} failed after {maxRetries} retries");
        }

        public bool IsRetryableError(Exception ex)
        {
            // Firebase-specific retryable errors
            var retryableErrorMessages = new[]
            {
                "timeout",
                "The connection was lost",
                "temporary failure",
                "Service Unavailable",
                "Too Many Requests",
                "500", // Internal Server Error
                "503", // Service Unavailable
            };

            var message = ex.Message.ToLower();
            return retryableErrorMessages.Any(msg => message.Contains(msg.ToLower()));
        }

        public void LogRetryAttempt(string operationName, int attempt, int maxRetries, Exception ex)
        {
            var delayMs = (int)(InitialDelayMs * Math.Pow(ExponentialBase, attempt - 1));
            _logger.LogWarning(
                "Retry attempt {Attempt}/{MaxRetries} for operation {OperationName}. " +
                "Error: {ErrorMessage}. Waiting {DelayMs}ms before retry",
                attempt, maxRetries, operationName, ex.Message, delayMs);
        }
    }
}

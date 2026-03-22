using Payment.Application.DTOs;

namespace Payment.Application.Interfaces;

public interface IWebhookHandler
{
    Task<bool> HandleWebhookAsync(string provider, IDictionary<string, string> queryParams, string correlationId);
}

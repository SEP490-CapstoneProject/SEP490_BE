using Payment.Application.DTOs;

namespace Payment.Application.Interfaces;

public interface IWebhookService
{
    Task<WebhookResult> ProcessWebhookAsync(string rawBody, string signature, string correlationId);
}

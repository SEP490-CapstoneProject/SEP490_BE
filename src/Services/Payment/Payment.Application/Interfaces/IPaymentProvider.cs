using Payment.Application.DTOs;
using Payment.Domain.Entities;

namespace Payment.Application.Interfaces;

public interface IPaymentProvider
{
    Task<string> CreatePaymentUrlAsync(PaymentEntity payment);
    WebhookResult ParseWebhookData(IDictionary<string, string> queryParams);
    bool ValidateSignature(IDictionary<string, string> queryParams);
}

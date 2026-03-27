using Payment.Domain.Entities;

namespace Payment.Domain.Interfaces;

public interface IPaymentHistoryRepository
{
    Task<PaymentHistory> CreateAsync(PaymentHistory history);
    Task<List<PaymentHistory>> GetByPaymentIdAsync(Guid paymentId);
    Task<List<PaymentHistory>> GetByCorrelationIdAsync(string correlationId);
}

using Payment.Domain.Entities;
using Payment.Domain.Enums;

namespace Payment.Domain.Interfaces;

public interface IPaymentRepository
{
    Task<PaymentEntity?> GetByIdAsync(Guid id);
    Task<PaymentEntity?> GetByOrderCodeAsync(string orderCode);
    Task<PaymentEntity?> GetPendingByUserAndPlanAsync(int userId, int planId);
    Task<List<PaymentEntity>> GetByUserIdAsync(int userId, int page = 1, int pageSize = 10);
    Task<List<PaymentEntity>> GetAllAsync(int? userId = null, PaymentStatus? status = null, DateTime? fromDate = null, DateTime? toDate = null, int page = 1, int pageSize = 20);
    Task<int> GetTotalCountAsync(int? userId = null, PaymentStatus? status = null, DateTime? fromDate = null, DateTime? toDate = null);
    Task<PaymentEntity> CreateAsync(PaymentEntity payment);
    Task<bool> UpdateStatusConditionalAsync(Guid paymentId, PaymentStatus newStatus, int expectedRowVersion, string? transactionId = null, DateTime? paidAt = null);
    Task<List<PaymentEntity>> GetExpiredPendingPaymentsAsync(DateTime threshold);
    Task BulkUpdateStatusAsync(List<Guid> paymentIds, PaymentStatus newStatus);
}

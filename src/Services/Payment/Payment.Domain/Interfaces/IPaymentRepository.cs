using Payment.Domain.Entities;
using Payment.Domain.Enums;
using System.Data;

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
    
    // Concurrency control methods
    Task<bool> UpdateStatusConditionalAsync(Guid paymentId, PaymentStatus newStatus, int expectedRowVersion);
    Task UpdateAsync(PaymentEntity payment);
    Task UpdateStatusAsync(Guid paymentId, PaymentStatus newStatus);
    
    // Reconciliation methods
    Task<List<PaymentEntity>> GetStuckPaymentsAsync(int minAgeMinutes, PaymentStatus[] statuses);
    Task<List<PaymentEntity>> GetExpiredPendingPaymentsAsync(DateTime threshold);
    Task BulkUpdateStatusAsync(List<Guid> paymentIds, PaymentStatus newStatus);
    
    // Transaction support
    Task<IDbTransaction> BeginTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted);
}

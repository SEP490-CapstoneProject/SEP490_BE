using Microsoft.EntityFrameworkCore;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Domain.Interfaces;
using Payment.Infrastructure.Data;

namespace Payment.Infrastructure.Repositories;

public class PaymentRepository : IPaymentRepository
{
    private readonly PaymentDbContext _context;

    public PaymentRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task<PaymentEntity?> GetByIdAsync(Guid id)
    {
        return await _context.Payments
            .Include(p => p.History)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<PaymentEntity?> GetByOrderCodeAsync(string orderCode)
    {
        return await _context.Payments
            .Include(p => p.History)
            .FirstOrDefaultAsync(p => p.OrderCode == orderCode);
    }

    public async Task<PaymentEntity?> GetPendingByUserAndPlanAsync(int userId, int planId)
    {
        return await _context.Payments
            .Include(p => p.History)
            .FirstOrDefaultAsync(p => 
                p.UserId == userId && 
                p.PlanId == planId && 
                p.Status == PaymentStatus.Pending);
    }

    public async Task<List<PaymentEntity>> GetByUserIdAsync(int userId, int page = 1, int pageSize = 10)
    {
        return await _context.Payments
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<List<PaymentEntity>> GetAllAsync(
        int? userId = null, 
        PaymentStatus? status = null, 
        DateTime? fromDate = null, 
        DateTime? toDate = null, 
        int page = 1, 
        int pageSize = 20)
    {
        var query = _context.Payments.AsQueryable();

        if (userId.HasValue)
            query = query.Where(p => p.UserId == userId.Value);

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        if (fromDate.HasValue)
            query = query.Where(p => p.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(p => p.CreatedAt <= toDate.Value);

        return await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<int> GetTotalCountAsync(
        int? userId = null, 
        PaymentStatus? status = null, 
        DateTime? fromDate = null, 
        DateTime? toDate = null)
    {
        var query = _context.Payments.AsQueryable();

        if (userId.HasValue)
            query = query.Where(p => p.UserId == userId.Value);

        if (status.HasValue)
            query = query.Where(p => p.Status == status.Value);

        if (fromDate.HasValue)
            query = query.Where(p => p.CreatedAt >= fromDate.Value);

        if (toDate.HasValue)
            query = query.Where(p => p.CreatedAt <= toDate.Value);

        return await query.CountAsync();
    }

    public async Task<PaymentEntity> CreateAsync(PaymentEntity payment)
    {
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();
        return payment;
    }

    public async Task<bool> UpdateStatusConditionalAsync(
        Guid paymentId, 
        PaymentStatus newStatus, 
        int expectedRowVersion, 
        string? transactionId = null, 
        DateTime? paidAt = null)
    {
        var rowsAffected = await _context.Database.ExecuteSqlRawAsync(
            @"UPDATE Payments 
              SET Status = {0}, 
                  UpdatedAt = GETUTCDATE(), 
                  RowVersion = RowVersion + 1,
                  TransactionId = COALESCE({1}, TransactionId),
                  PaidAt = COALESCE({2}, PaidAt)
              WHERE Id = {3} 
                AND Status IN (0, 1) 
                AND RowVersion = {4}",
            (int)newStatus,
            (object?)transactionId ?? DBNull.Value,
            (object?)paidAt ?? DBNull.Value,
            paymentId,
            expectedRowVersion);

        return rowsAffected > 0;
    }

    public async Task<List<PaymentEntity>> GetExpiredPendingPaymentsAsync(DateTime threshold)
    {
        return await _context.Payments
            .Where(p => 
                p.Status == PaymentStatus.Pending && 
                p.ExpiresAt.HasValue && 
                p.ExpiresAt.Value < threshold)
            .ToListAsync();
    }

    public async Task BulkUpdateStatusAsync(List<Guid> paymentIds, PaymentStatus newStatus)
    {
        await _context.Payments
            .Where(p => paymentIds.Contains(p.Id))
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.Status, newStatus)
                .SetProperty(p => p.UpdatedAt, DateTime.UtcNow)
                .SetProperty(p => p.RowVersion, p => p.RowVersion + 1));
    }
}

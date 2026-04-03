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
        byte[] expectedRowVersion)
    {
        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.Id == paymentId);

        if (payment == null) return false;

        // Check RowVersion for optimistic concurrency
        if (!payment.RowVersion.SequenceEqual(expectedRowVersion))
        {
            return false; // Another process updated it
        }

        payment.Status = newStatus;
        payment.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false; // Race condition
        }
    }

    public async Task UpdateAsync(PaymentEntity payment)
    {
        _context.Payments.Update(payment);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateStatusAsync(Guid paymentId, PaymentStatus newStatus)
    {
        var payment = await _context.Payments.FindAsync(paymentId);
        if (payment != null)
        {
            payment.Status = newStatus;
            payment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<List<PaymentEntity>> GetStuckPaymentsAsync(int minAgeMinutes, PaymentStatus[] statuses)
    {
        var threshold = DateTime.UtcNow.AddMinutes(-minAgeMinutes);
        return await _context.Payments
            .Where(p => statuses.Contains(p.Status) && p.CreatedAt < threshold)
            .ToListAsync();
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
                .SetProperty(p => p.UpdatedAt, DateTime.UtcNow));
    }

    public async Task<System.Data.IDbTransaction> BeginTransactionAsync(System.Data.IsolationLevel isolationLevel = System.Data.IsolationLevel.ReadCommitted)
    {
        var transaction = await _context.Database.BeginTransactionAsync();
        return new TransactionWrapper(transaction);
    }
}

/// <summary>
/// Wrapper for EF Core transaction to implement IDbTransaction
/// </summary>
internal class TransactionWrapper : System.Data.IDbTransaction
{
    private readonly Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction _transaction;
    
    public TransactionWrapper(Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction)
    {
        _transaction = transaction;
    }
    
    public System.Data.IDbConnection? Connection => null;
    public System.Data.IsolationLevel IsolationLevel => System.Data.IsolationLevel.ReadCommitted;
    
    public void Commit() => _transaction.Commit();
    public void Rollback() => _transaction.Rollback();
    public void Dispose() => _transaction.Dispose();
    
    public async Task CommitAsync() => await _transaction.CommitAsync();
    public async Task RollbackAsync() => await _transaction.RollbackAsync();
}

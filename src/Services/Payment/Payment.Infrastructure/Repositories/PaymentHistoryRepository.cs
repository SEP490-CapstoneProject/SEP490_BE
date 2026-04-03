using Microsoft.EntityFrameworkCore;
using Payment.Domain.Entities;
using Payment.Domain.Interfaces;
using Payment.Infrastructure.Data;

namespace Payment.Infrastructure.Repositories;

public class PaymentHistoryRepository : IPaymentHistoryRepository
{
    private readonly PaymentDbContext _context;

    public PaymentHistoryRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task<PaymentHistory> CreateAsync(PaymentHistory history)
    {
        _context.PaymentHistories.Add(history);
        await _context.SaveChangesAsync();
        return history;
    }

    public async Task<List<PaymentHistory>> GetByPaymentIdAsync(Guid paymentId)
    {
        return await _context.PaymentHistories
            .Where(h => h.PaymentId == paymentId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<PaymentHistory>> GetByCorrelationIdAsync(string correlationId)
    {
        return await _context.PaymentHistories
            .Where(h => h.CorrelationId == correlationId)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync();
    }
}

using Microsoft.EntityFrameworkCore;
using Payment.Domain.Entities;
using Payment.Domain.Interfaces;
using Payment.Infrastructure.Data;

namespace Payment.Infrastructure.Repositories;

public class ProcessedEventRepository : IProcessedEventRepository
{
    private readonly PaymentDbContext _context;

    public ProcessedEventRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task<bool> IsProcessedByHashAsync(string eventHash)
    {
        return await _context.ProcessedEvents
            .AnyAsync(e => e.EventHash == eventHash);
    }

    public async Task<bool> IsProcessedByOrderCodeAsync(string orderCode)
    {
        return await _context.ProcessedEvents
            .AnyAsync(e => e.OrderCode == orderCode);
    }

    public async Task CreateAsync(ProcessedEvent entity)
    {
        _context.ProcessedEvents.Add(entity);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteOlderThanAsync(DateTime threshold)
    {
        await _context.ProcessedEvents
            .Where(e => e.ProcessedAt < threshold)
            .ExecuteDeleteAsync();
    }
}

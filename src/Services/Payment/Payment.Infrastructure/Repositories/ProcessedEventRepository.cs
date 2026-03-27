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

    public async Task<bool> IsProcessedAsync(string eventId)
    {
        return await _context.ProcessedEvents
            .AnyAsync(e => e.EventId == eventId);
    }

    public async Task<bool> IsProcessedByHashAsync(string rawHash)
    {
        return await _context.ProcessedEvents
            .AnyAsync(e => e.RawHash == rawHash);
    }

    public async Task<ProcessedEvent> MarkAsProcessedAsync(ProcessedEvent processedEvent)
    {
        _context.ProcessedEvents.Add(processedEvent);
        await _context.SaveChangesAsync();
        return processedEvent;
    }

    public async Task<ProcessedEvent?> GetByEventIdAsync(string eventId)
    {
        return await _context.ProcessedEvents
            .FirstOrDefaultAsync(e => e.EventId == eventId);
    }

    public async Task DeleteOlderThanAsync(DateTime threshold)
    {
        await _context.ProcessedEvents
            .Where(e => e.ProcessedAt < threshold)
            .ExecuteDeleteAsync();
    }
}

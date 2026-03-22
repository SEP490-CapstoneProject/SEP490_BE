using Microsoft.EntityFrameworkCore;
using Subscription.Application.Interfaces;
using Subscription.Domain.Entities;
using Subscription.Infrastructure.Data;

namespace Subscription.Infrastructure.Repositories;

public class ProcessedEventRepository : IProcessedEventRepository
{
    private readonly SubscriptionDbContext _context;

    public ProcessedEventRepository(SubscriptionDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsAsync(string eventId)
    {
        return await _context.ProcessedEvents.AnyAsync(e => e.EventId == eventId);
    }

    public async Task AddAsync(ProcessedEvent processedEvent)
    {
        processedEvent.ProcessedAt = DateTime.UtcNow;
        _context.ProcessedEvents.Add(processedEvent);
        await _context.SaveChangesAsync();
    }

    public async Task<ProcessedEvent?> GetByIdAsync(string eventId)
    {
        return await _context.ProcessedEvents.FindAsync(eventId);
    }
}

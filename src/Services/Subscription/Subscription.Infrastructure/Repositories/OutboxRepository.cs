using Microsoft.EntityFrameworkCore;
using Subscription.Application.Interfaces;
using Subscription.Domain.Entities;
using Subscription.Domain.Enums;
using Subscription.Infrastructure.Data;

namespace Subscription.Infrastructure.Repositories;

public class OutboxRepository : IOutboxRepository
{
    private readonly SubscriptionDbContext _context;

    public OutboxRepository(SubscriptionDbContext context)
    {
        _context = context;
    }

    public async Task<OutboxEvent> AddAsync(OutboxEvent outboxEvent)
    {
        outboxEvent.CreatedAt = DateTime.UtcNow;
        outboxEvent.Status = OutboxStatus.Pending;
        _context.OutboxEvents.Add(outboxEvent);
        await _context.SaveChangesAsync();
        return outboxEvent;
    }

    public async Task<IEnumerable<OutboxEvent>> GetPendingAsync(int limit = 100)
    {
        return await _context.OutboxEvents
            .Where(e => e.Status == OutboxStatus.Pending &&
                        e.EventType.StartsWith("subscription."))
            .OrderBy(e => e.CreatedAt)
            .Take(limit)
            .ToListAsync();
    }

    public async Task UpdateStatusAsync(int id, OutboxStatus status, string? errorMessage = null)
    {
        var entity = await _context.OutboxEvents.FindAsync(id);
        if (entity != null)
        {
            entity.Status = status;
            entity.ErrorMessage = errorMessage;
            if (status == OutboxStatus.Published)
            {
                entity.ProcessedAt = DateTime.UtcNow;
            }
            await _context.SaveChangesAsync();
        }
    }

    public async Task MarkAsPublishedAsync(int id)
    {
        await UpdateStatusAsync(id, OutboxStatus.Published);
    }

    public async Task IncrementRetryCountAsync(int id, string errorMessage)
    {
        var entity = await _context.OutboxEvents.FindAsync(id);
        if (entity != null)
        {
            entity.RetryCount++;
            entity.ErrorMessage = errorMessage;
            if (entity.RetryCount >= 5)
            {
                entity.Status = OutboxStatus.Failed;
            }
            await _context.SaveChangesAsync();
        }
    }
}

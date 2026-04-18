using Microsoft.EntityFrameworkCore;
using Payment.Domain.Entities;
using Payment.Domain.Enums;
using Payment.Domain.Interfaces;
using Payment.Infrastructure.Data;

namespace Payment.Infrastructure.Repositories;

public class OutboxEventRepository : IOutboxEventRepository
{
    private readonly PaymentDbContext _context;
    private const int MaxRetries = 5;

    public OutboxEventRepository(PaymentDbContext context)
    {
        _context = context;
    }

    public async Task<OutboxEvent> CreateAsync(OutboxEvent outboxEvent)
    {
        _context.OutboxEvents.Add(outboxEvent);
        await _context.SaveChangesAsync();
        return outboxEvent;
    }

    public async Task<List<OutboxEvent>> GetPendingEventsAsync(int batchSize = 10)
    {
        return await _context.OutboxEvents
            .Where(e => e.Status == OutboxStatus.Pending && e.EventType == "PaymentSucceeded")
            .OrderBy(e => e.CreatedAt)
            .Take(batchSize)
            .ToListAsync();
    }

    public async Task<List<OutboxEvent>> GetEventsToRetryAsync(int batchSize = 10)
    {
        var now = DateTime.UtcNow;
        
        return await _context.OutboxEvents
            .Where(e => 
                e.Status == OutboxStatus.Failed && 
                e.NextRetryAt.HasValue && 
                e.NextRetryAt.Value <= now &&
                e.RetryCount < MaxRetries)
            .OrderBy(e => e.NextRetryAt)
            .Take(batchSize)
            .ToListAsync();
    }

    public async Task<bool> UpdateStatusAsync(int id, OutboxStatus status, string? lastError = null)
    {
        var outboxEvent = await _context.OutboxEvents.FindAsync(id);
        if (outboxEvent == null)
            return false;

        outboxEvent.Status = status;
        if (lastError != null)
            outboxEvent.LastError = lastError;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> IncrementRetryAsync(int id, DateTime nextRetryAt, string? lastError = null)
    {
        var outboxEvent = await _context.OutboxEvents.FindAsync(id);
        if (outboxEvent == null)
            return false;

        outboxEvent.RetryCount++;
        outboxEvent.NextRetryAt = nextRetryAt;
        outboxEvent.Status = OutboxStatus.Failed;
        
        if (lastError != null)
            outboxEvent.LastError = lastError;

        // Move to DeadLetter if max retries exceeded
        if (outboxEvent.RetryCount >= MaxRetries)
        {
            outboxEvent.Status = OutboxStatus.DeadLetter;
            outboxEvent.LastError = $"Max retries ({MaxRetries}) exceeded. Last error: {lastError}";
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> MarkAsPublishedAsync(int id)
    {
        return await UpdateStatusAsync(id, OutboxStatus.Published);
    }

    public async Task<bool> MarkAsDeadLetterAsync(int id, string lastError)
    {
        return await UpdateStatusAsync(id, OutboxStatus.DeadLetter, lastError);
    }

    public async Task<OutboxEvent?> GetByIdAsync(int id)
    {
        return await _context.OutboxEvents.FindAsync(id);
    }

    public async Task DeleteOlderThanAsync(DateTime threshold, OutboxStatus status)
    {
        await _context.OutboxEvents
            .Where(e => e.CreatedAt < threshold && e.Status == status)
            .ExecuteDeleteAsync();
    }
}

using Payment.Domain.Entities;
using Payment.Domain.Enums;

namespace Payment.Domain.Interfaces;

public interface IOutboxEventRepository
{
    Task<OutboxEvent> CreateAsync(OutboxEvent outboxEvent);
    Task<List<OutboxEvent>> GetPendingEventsAsync(int batchSize = 10);
    Task<List<OutboxEvent>> GetEventsToRetryAsync(int batchSize = 10);
    Task<bool> UpdateStatusAsync(int id, OutboxStatus status, string? lastError = null);
    Task<bool> IncrementRetryAsync(int id, DateTime nextRetryAt, string? lastError = null);
    Task<bool> MarkAsPublishedAsync(int id);
    Task<bool> MarkAsDeadLetterAsync(int id, string lastError);
    Task<OutboxEvent?> GetByIdAsync(int id);
    Task DeleteOlderThanAsync(DateTime threshold, OutboxStatus status);
}

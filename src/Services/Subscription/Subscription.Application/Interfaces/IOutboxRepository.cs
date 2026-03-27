using Subscription.Domain.Entities;
using Subscription.Domain.Enums;

namespace Subscription.Application.Interfaces;

public interface IOutboxRepository
{
    Task<OutboxEvent> AddAsync(OutboxEvent outboxEvent);
    Task<IEnumerable<OutboxEvent>> GetPendingAsync(int limit = 100);
    Task UpdateStatusAsync(int id, OutboxStatus status, string? errorMessage = null);
    Task MarkAsPublishedAsync(int id);
    Task IncrementRetryCountAsync(int id, string errorMessage);
}

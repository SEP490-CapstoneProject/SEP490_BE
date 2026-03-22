using Subscription.Domain.Entities;

namespace Subscription.Application.Interfaces;

public interface IProcessedEventRepository
{
    Task<bool> ExistsAsync(string eventId);
    Task AddAsync(ProcessedEvent processedEvent);
    Task<ProcessedEvent?> GetByIdAsync(string eventId);
}

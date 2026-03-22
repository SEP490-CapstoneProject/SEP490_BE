using Payment.Domain.Entities;

namespace Payment.Domain.Interfaces;

public interface IProcessedEventRepository
{
    Task<bool> IsProcessedAsync(string eventId);
    Task<bool> IsProcessedByHashAsync(string rawHash);
    Task<ProcessedEvent> MarkAsProcessedAsync(ProcessedEvent processedEvent);
    Task<ProcessedEvent?> GetByEventIdAsync(string eventId);
    Task DeleteOlderThanAsync(DateTime threshold);
}

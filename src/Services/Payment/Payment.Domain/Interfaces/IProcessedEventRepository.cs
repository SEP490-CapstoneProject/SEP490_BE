using Payment.Domain.Entities;

namespace Payment.Domain.Interfaces;

public interface IProcessedEventRepository
{
    Task<bool> IsProcessedByHashAsync(string eventHash);
    Task<bool> IsProcessedByOrderCodeAsync(string orderCode);
    Task CreateAsync(ProcessedEvent entity);
    Task DeleteOlderThanAsync(DateTime threshold);
}

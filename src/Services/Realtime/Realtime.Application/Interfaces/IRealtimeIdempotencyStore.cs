namespace Realtime.Application.Interfaces;

public interface IRealtimeIdempotencyStore
{
    Task<bool> TryBeginProcessingAsync(string eventId, TimeSpan ttl, CancellationToken cancellationToken = default);
}

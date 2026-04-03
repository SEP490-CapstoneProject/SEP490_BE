using System.Collections.Concurrent;
using Realtime.Application.Interfaces;

namespace Realtime.Infrastructure.Services;

public class MemoryRealtimeIdempotencyStore : IRealtimeIdempotencyStore
{
    private readonly ConcurrentDictionary<string, DateTime> _entries = new();

    public Task<bool> TryBeginProcessingAsync(string eventId, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var expiry = now.Add(ttl);
        var key = $"event:{eventId}";

        while (true)
        {
            if (_entries.TryGetValue(key, out var existingExpiry))
            {
                if (existingExpiry > now)
                {
                    return Task.FromResult(false);
                }

                _entries.TryRemove(key, out _);
                continue;
            }

            if (_entries.TryAdd(key, expiry))
            {
                return Task.FromResult(true);
            }
        }
    }
}

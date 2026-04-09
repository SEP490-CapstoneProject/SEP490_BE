using Realtime.Application.Interfaces;
using StackExchange.Redis;

namespace Realtime.Infrastructure.Services;

public class RedisRealtimeIdempotencyStore : IRealtimeIdempotencyStore
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;

    public RedisRealtimeIdempotencyStore(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
    }

    public async Task<bool> TryBeginProcessingAsync(string eventId, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        var key = $"event:{eventId}";
        var db = _connectionMultiplexer.GetDatabase();
        return await db.StringSetAsync(key, "1", ttl, when: When.NotExists);
    }
}

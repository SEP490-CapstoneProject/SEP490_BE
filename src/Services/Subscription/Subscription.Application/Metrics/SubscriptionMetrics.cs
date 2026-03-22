using System.Diagnostics.Metrics;

namespace Subscription.Application.Metrics;

public sealed class SubscriptionMetrics
{
    private readonly Counter<long> _redisHits;
    private readonly Counter<long> _redisMisses;
    private readonly Counter<long> _fallbackCalls;
    private readonly Counter<long> _quotaExceeded;
    private readonly Counter<long> _eventsPublished;
    private readonly Counter<long> _eventsFailed;
    private readonly Counter<long> _duplicateEvents;
    private readonly Histogram<double> _operationDuration;

    public SubscriptionMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("Subscription.Service");

        _redisHits = meter.CreateCounter<long>(
            "subscription.redis.hits",
            description: "Number of Redis cache hits");

        _redisMisses = meter.CreateCounter<long>(
            "subscription.redis.misses",
            description: "Number of Redis cache misses");

        _fallbackCalls = meter.CreateCounter<long>(
            "subscription.fallback.calls",
            description: "Number of fallback calls to default tier");

        _quotaExceeded = meter.CreateCounter<long>(
            "subscription.quota.exceeded",
            description: "Number of quota exceeded events");

        _eventsPublished = meter.CreateCounter<long>(
            "subscription.events.published",
            description: "Number of events published");

        _eventsFailed = meter.CreateCounter<long>(
            "subscription.events.failed",
            description: "Number of failed event publishes");

        _duplicateEvents = meter.CreateCounter<long>(
            "subscription.events.duplicates",
            description: "Number of duplicate events skipped");

        _operationDuration = meter.CreateHistogram<double>(
            "subscription.operation.duration",
            unit: "ms",
            description: "Duration of subscription operations");
    }

    public void RecordRedisHit() => _redisHits.Add(1);
    public void RecordRedisMiss() => _redisMisses.Add(1);
    public void RecordFallback() => _fallbackCalls.Add(1);
    public void RecordQuotaExceeded() => _quotaExceeded.Add(1);
    public void RecordEventPublished() => _eventsPublished.Add(1);
    public void RecordEventFailed() => _eventsFailed.Add(1);
    public void RecordDuplicateEvent() => _duplicateEvents.Add(1);
    
    public void RecordOperationDuration(string operation, double durationMs)
    {
        _operationDuration.Record(durationMs, new KeyValuePair<string, object?>("operation", operation));
    }
}

using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using StackExchange.Redis;

namespace Subscription.Application.Policies;

public static class ResiliencePolicies
{
    public static readonly ResiliencePipeline<RedisValue> RedisPipeline = new ResiliencePipelineBuilder<RedisValue>()
        .AddRetry(new RetryStrategyOptions<RedisValue>
        {
            ShouldHandle = new PredicateBuilder<RedisValue>()
                .Handle<RedisConnectionException>()
                .Handle<RedisTimeoutException>(),
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(100),
            BackoffType = DelayBackoffType.Exponential
        })
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions<RedisValue>
        {
            FailureRatio = 0.5,
            SamplingDuration = TimeSpan.FromSeconds(30),
            MinimumThroughput = 5,
            BreakDuration = TimeSpan.FromSeconds(30)
        })
        .AddTimeout(TimeSpan.FromSeconds(5))
        .Build();

    public static readonly ResiliencePipeline RabbitMQPipeline = new ResiliencePipelineBuilder()
        .AddRetry(new RetryStrategyOptions
        {
            ShouldHandle = new PredicateBuilder().Handle<Exception>(),
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromMilliseconds(200),
            BackoffType = DelayBackoffType.Exponential
        })
        .AddCircuitBreaker(new CircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            SamplingDuration = TimeSpan.FromSeconds(30),
            MinimumThroughput = 5,
            BreakDuration = TimeSpan.FromMinutes(1)
        })
        .Build();
}

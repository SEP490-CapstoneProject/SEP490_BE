using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Realtime.Application.Interfaces;
using RecruitmentPlatform.Contracts.Realtime;

namespace Realtime.Infrastructure.Messaging;

public class SkillPointsAwardedEventConsumer : RealtimeConsumerBase<SkillPointsAwardedEvent>
{
    public SkillPointsAwardedEventConsumer(
        IConfiguration configuration,
        IRealtimePushService pushService,
        IRealtimeIdempotencyStore idempotencyStore,
        ILogger<SkillPointsAwardedEventConsumer> logger)
        : base(configuration, pushService, idempotencyStore, logger)
    {
    }

    protected override string QueueName => "realtime.skillpoints.awarded";
    protected override string RoutingKey => "skillpoints.awarded";

    protected override Task PushAsync(SkillPointsAwardedEvent evt, CancellationToken cancellationToken)
        => PushService.PushSkillPointsAwardedAsync(evt, cancellationToken);
}

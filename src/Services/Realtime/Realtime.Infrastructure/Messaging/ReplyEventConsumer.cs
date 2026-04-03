using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Realtime.Application.Interfaces;
using RecruitmentPlatform.Contracts.Realtime;

namespace Realtime.Infrastructure.Messaging;

public class ReplyEventConsumer : RealtimeConsumerBase<ReplyCreatedEvent>
{
    public ReplyEventConsumer(
        IConfiguration configuration,
        IRealtimePushService pushService,
        IRealtimeIdempotencyStore idempotencyStore,
        ILogger<ReplyEventConsumer> logger)
        : base(configuration, pushService, idempotencyStore, logger)
    {
    }

    protected override string QueueName => "realtime.reply.events";
    protected override string RoutingKey => "post.reply.created";

    protected override Task PushAsync(ReplyCreatedEvent evt, CancellationToken cancellationToken)
        => PushService.PushReplyAsync(evt, cancellationToken);
}

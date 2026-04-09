using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Realtime.Application.Interfaces;
using RecruitmentPlatform.Contracts.Realtime;

namespace Realtime.Infrastructure.Messaging;

public class CommentEventConsumer : RealtimeConsumerBase<CommentCreatedEvent>
{
    public CommentEventConsumer(
        IConfiguration configuration,
        IRealtimePushService pushService,
        IRealtimeIdempotencyStore idempotencyStore,
        ILogger<CommentEventConsumer> logger)
        : base(configuration, pushService, idempotencyStore, logger)
    {
    }

    protected override string QueueName => "realtime.comment.events";
    protected override string RoutingKey => "post.comment.created";

    protected override Task PushAsync(CommentCreatedEvent evt, CancellationToken cancellationToken)
        => PushService.PushCommentAsync(evt, cancellationToken);
}

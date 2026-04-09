using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Realtime.Application.Interfaces;
using RecruitmentPlatform.Contracts.Realtime;

namespace Realtime.Infrastructure.Messaging;

public class PostFavoriteEventConsumer : RealtimeConsumerBase<PostFavoriteChangedEvent>
{
    public PostFavoriteEventConsumer(
        IConfiguration configuration,
        IRealtimePushService pushService,
        IRealtimeIdempotencyStore idempotencyStore,
        ILogger<PostFavoriteEventConsumer> logger)
        : base(configuration, pushService, idempotencyStore, logger)
    {
    }

    protected override string QueueName => "realtime.post.favorite.events";
    protected override string RoutingKey => "post.favorite.changed";

    protected override Task PushAsync(PostFavoriteChangedEvent evt, CancellationToken cancellationToken)
        => PushService.PushPostFavoriteChangedAsync(evt, cancellationToken);
}

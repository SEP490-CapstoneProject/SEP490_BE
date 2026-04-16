using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Realtime.Application.Interfaces;
using RecruitmentPlatform.Contracts.Realtime;

namespace Realtime.Infrastructure.Messaging;

public class ConnectionAcceptedEventConsumer : RealtimeConsumerBase<ConnectionAcceptedEvent>
{
    public ConnectionAcceptedEventConsumer(
        IConfiguration configuration,
        IRealtimePushService pushService,
        IRealtimeIdempotencyStore idempotencyStore,
        ILogger<ConnectionAcceptedEventConsumer> logger)
        : base(configuration, pushService, idempotencyStore, logger)
    {
    }

    protected override string QueueName => "realtime.connection.accepted";
    protected override string RoutingKey => "connection.accepted";

    protected override Task PushAsync(ConnectionAcceptedEvent evt, CancellationToken cancellationToken)
        => PushService.PushConnectionAcceptedAsync(evt, cancellationToken);
}

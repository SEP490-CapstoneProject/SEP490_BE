using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Realtime.Application.Interfaces;
using RecruitmentPlatform.Contracts.Realtime;

namespace Realtime.Infrastructure.Messaging;

public class ConnectionRequestedEventConsumer : RealtimeConsumerBase<ConnectionRequestedEvent>
{
    public ConnectionRequestedEventConsumer(
        IConfiguration configuration,
        IRealtimePushService pushService,
        IRealtimeIdempotencyStore idempotencyStore,
        ILogger<ConnectionRequestedEventConsumer> logger)
        : base(configuration, pushService, idempotencyStore, logger)
    {
    }

    protected override string QueueName => "realtime.connection.requested";
    protected override string RoutingKey => "connection.requested";

    protected override Task PushAsync(ConnectionRequestedEvent evt, CancellationToken cancellationToken)
        => PushService.PushConnectionRequestedAsync(evt, cancellationToken);
}

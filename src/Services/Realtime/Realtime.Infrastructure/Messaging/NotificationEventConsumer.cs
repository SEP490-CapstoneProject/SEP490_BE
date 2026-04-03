using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Realtime.Application.Interfaces;
using RecruitmentPlatform.Contracts.Realtime;

namespace Realtime.Infrastructure.Messaging;

public class NotificationEventConsumer : RealtimeConsumerBase<NotificationCreatedEvent>
{
    public NotificationEventConsumer(
        IConfiguration configuration,
        IRealtimePushService pushService,
        IRealtimeIdempotencyStore idempotencyStore,
        ILogger<NotificationEventConsumer> logger)
        : base(configuration, pushService, idempotencyStore, logger)
    {
    }

    protected override string QueueName => "realtime.notification.events";
    protected override string RoutingKey => "notification.created";

    protected override Task PushAsync(NotificationCreatedEvent evt, CancellationToken cancellationToken)
        => PushService.PushNotificationAsync(evt, cancellationToken);
}

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Realtime.Application.Interfaces;
using RecruitmentPlatform.Contracts.Realtime;

namespace Realtime.Infrastructure.Messaging;

public class NewMessageNotificationEventConsumer : RealtimeConsumerBase<NewMessageNotificationEvent>
{
    private readonly NewMessageDebouncer _debouncer;

    public NewMessageNotificationEventConsumer(
        IConfiguration configuration,
        IRealtimePushService pushService,
        IRealtimeIdempotencyStore idempotencyStore,
        NewMessageDebouncer debouncer,
        ILogger<NewMessageNotificationEventConsumer> logger)
        : base(configuration, pushService, idempotencyStore, logger)
    {
        _debouncer = debouncer;
    }

    protected override string QueueName => "realtime.message.new";
    protected override string RoutingKey => "message.new";

    /// <summary>
    /// Push ngay qua SignalR với đầy đủ thông tin người gửi (name, avatar, roomId, role, sentAt, content).
    /// Đồng thời thêm vào FCM debouncer để gom count và gửi offline push sau 2s.
    /// </summary>
    protected override Task PushAsync(NewMessageNotificationEvent evt, CancellationToken cancellationToken)
    {
        // 1. Push ngay qua SignalR (realtime) using existing new-message channel to preserve flow
        _ = PushService.PushNewMessageNotificationAsync(evt, cancellationToken);

        // 2. Gom vào FCM debouncer (offline push sau 2s). Notification service still performs FCM sends; debouncer optional.
        _debouncer.Add(evt);

        return Task.CompletedTask;
    }
}

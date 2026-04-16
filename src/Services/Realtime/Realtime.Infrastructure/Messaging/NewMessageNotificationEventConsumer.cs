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
    /// Thay vì push thẳng, thêm vào debouncer.
    /// Debouncer sẽ gom các event trong 2 giây rồi push 1 lần duy nhất với số đếm tổng.
    /// </summary>
    protected override Task PushAsync(NewMessageNotificationEvent evt, CancellationToken cancellationToken)
    {
        _debouncer.Add(evt);
        return Task.CompletedTask;
    }
}

using Microsoft.AspNetCore.SignalR;
using Realtime.API.Hubs;
using Realtime.Application.Interfaces;
using RecruitmentPlatform.Contracts.Realtime;

namespace Realtime.API.Services;

public class SignalRPushService : IRealtimePushService
{
    private readonly IHubContext<RealtimeHub> _hubContext;

    public SignalRPushService(IHubContext<RealtimeHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task PushNotificationAsync(NotificationCreatedEvent evt, CancellationToken cancellationToken = default)
        => _hubContext.Clients
            .Group($"user_{evt.UserId}")
            .SendAsync("ReceiveNotification", evt, cancellationToken);

    public Task PushCommentAsync(CommentCreatedEvent evt, CancellationToken cancellationToken = default)
        => _hubContext.Clients
            .Group($"post_{evt.PostId}")
            .SendAsync("ReceiveComment", evt, cancellationToken);

    public Task PushReplyAsync(ReplyCreatedEvent evt, CancellationToken cancellationToken = default)
        => _hubContext.Clients
            .Group($"post_{evt.PostId}")
            .SendAsync("ReceiveReply", evt, cancellationToken);

    public Task PushPostFavoriteChangedAsync(PostFavoriteChangedEvent evt, CancellationToken cancellationToken = default)
        => _hubContext.Clients
            .Group($"post_{evt.PostId}")
            .SendAsync("ReceivePostFavoriteChanged", evt, cancellationToken);
}

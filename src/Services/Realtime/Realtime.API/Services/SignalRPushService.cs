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

    /// <summary>
    /// Notify user B (ToUserId) that user A sent a connection request.
    /// FE listens: connection.on("ConnectionRequested", handler)
    /// </summary>
    public Task PushConnectionRequestedAsync(ConnectionRequestedEvent evt, CancellationToken cancellationToken = default)
        => _hubContext.Clients
            .Group($"user_{evt.ToUserId}")
            .SendAsync("ConnectionRequested", evt, cancellationToken);

    /// <summary>
    /// Notify user A (FromUserId) that user B accepted the connection.
    /// FE listens: connection.on("ConnectionAccepted", handler)
    /// </summary>
    public Task PushConnectionAcceptedAsync(ConnectionAcceptedEvent evt, CancellationToken cancellationToken = default)
        => _hubContext.Clients
            .Group($"user_{evt.FromUserId}")
            .SendAsync("ConnectionAccepted", evt, cancellationToken);

    /// <summary>
    /// Push tổng số tin nhắn mới cho user (gom từ tất cả room, debounced 2s).
    /// FE listens: connection.on("NewMessageNotification", data => data.totalNewMessages)
    /// </summary>
    public Task PushNewMessageNotificationAsync(int toUserId, int totalNewMessages, CancellationToken cancellationToken = default)
        => _hubContext.Clients
            .Group($"user_{toUserId}")
            .SendAsync("NewMessageNotification", new
            {
                toUserId,
                totalNewMessages
            }, cancellationToken);
}

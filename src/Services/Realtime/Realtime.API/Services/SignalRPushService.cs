using Microsoft.AspNetCore.SignalR;
using Realtime.API.Hubs;
using Realtime.Application.Interfaces;
using RecruitmentPlatform.Contracts.Realtime;

namespace Realtime.API.Services;

public class SignalRPushService : IRealtimePushService
{
    private readonly IHubContext<RealtimeHub> _hubContext;
    private static readonly HashSet<string> CommunityTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "COMMUNITY",
        "POST_FAVORITE",
        "COMMUNITY_REPORT_REVIEW"
    };

    public SignalRPushService(IHubContext<RealtimeHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task PushNotificationAsync(NotificationCreatedEvent evt, CancellationToken cancellationToken = default)
    {
        var tasks = new List<Task>
        {
            _hubContext.Clients
                .Group($"user_{evt.UserId}")
                .SendAsync("ReceiveNotification", evt, cancellationToken)
        };

        if (IsCommunity(evt))
        {
            tasks.Add(PushCommunityNotificationAsync(evt, cancellationToken));
        }
        else
        {
            tasks.Add(PushSystemNotificationAsync(evt, cancellationToken));
        }

        return Task.WhenAll(tasks);
    }

    public Task PushCommunityNotificationAsync(NotificationCreatedEvent evt, CancellationToken cancellationToken = default)
        => _hubContext.Clients
            .Group($"user_{evt.UserId}")
            .SendAsync("ReceiveCommunityNotification", evt, cancellationToken);

    public Task PushSystemNotificationAsync(NotificationCreatedEvent evt, CancellationToken cancellationToken = default)
        => _hubContext.Clients
            .Group($"user_{evt.UserId}")
            .SendAsync("ReceiveSystemNotification", evt, cancellationToken);

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
    /// Push thông báo tin nhắn mới với đầy đủ thông tin người gửi.
    /// FE listens: connection.on("NewMessageNotification", data => { data.roomId, data.sender.name, data.sender.avatar, ... })
    /// </summary>
    public Task PushNewMessageNotificationAsync(NewMessageNotificationEvent evt, CancellationToken cancellationToken = default)
        => _hubContext.Clients
            .Group($"user_{evt.ToUserId}")
            .SendAsync("NewMessageNotification", new
            {
                messageId   = evt.MessageId,
                roomId      = evt.RoomId,
                content     = evt.Content,
                sentAt      = evt.SentAt,
                sender = evt.Author == null ? null : new
                {
                    id     = evt.Author.Id,
                    name   = evt.Author.Name,
                    avatar = evt.Author.Avatar,
                    role   = evt.Author.Role
                }
            }, cancellationToken);

    private static bool IsCommunity(NotificationCreatedEvent evt)
    {
        if (string.Equals(evt.Category, "community", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return CommunityTypes.Contains(evt.Type);
    }
}

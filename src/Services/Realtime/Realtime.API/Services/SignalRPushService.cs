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
            _hubContext.Clients.Group($"user_{evt.UserId}").SendAsync("ReceiveNotification", evt, cancellationToken)
        };

        tasks.Add(IsCommunity(evt)
            ? PushCommunityNotificationAsync(evt, cancellationToken)
            : PushSystemNotificationAsync(evt, cancellationToken));

        return Task.WhenAll(tasks);
    }

    public Task PushCommunityNotificationAsync(NotificationCreatedEvent evt, CancellationToken cancellationToken = default)
        => _hubContext.Clients.Group($"user_{evt.UserId}").SendAsync("ReceiveCommunityNotification", evt, cancellationToken);

    public Task PushSystemNotificationAsync(NotificationCreatedEvent evt, CancellationToken cancellationToken = default)
        => _hubContext.Clients.Group($"user_{evt.UserId}").SendAsync("ReceiveSystemNotification", evt, cancellationToken);

    public Task PushCommentAsync(CommentCreatedEvent evt, CancellationToken cancellationToken = default)
        => _hubContext.Clients.Group($"post_{evt.PostId}").SendAsync("ReceiveComment", evt, cancellationToken);

    public Task PushReplyAsync(ReplyCreatedEvent evt, CancellationToken cancellationToken = default)
        => _hubContext.Clients.Group($"post_{evt.PostId}").SendAsync("ReceiveReply", evt, cancellationToken);

    public Task PushPostFavoriteChangedAsync(PostFavoriteChangedEvent evt, CancellationToken cancellationToken = default)
        => _hubContext.Clients.Group($"post_{evt.PostId}").SendAsync("ReceivePostFavoriteChanged", evt, cancellationToken);

    public Task PushConnectionRequestedAsync(ConnectionRequestedEvent evt, CancellationToken cancellationToken = default)
        => _hubContext.Clients.Group($"user_{evt.ToUserId}").SendAsync("ConnectionRequested", evt, cancellationToken);

    public Task PushConnectionAcceptedAsync(ConnectionAcceptedEvent evt, CancellationToken cancellationToken = default)
        => _hubContext.Clients.Group($"user_{evt.FromUserId}").SendAsync("ConnectionAccepted", evt, cancellationToken);

    public Task PushSkillPointsAwardedAsync(SkillPointsAwardedEvent evt, CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            userId = evt.UserId,
            challengeId = evt.ChallengeId,
            skillPoints = evt.SkillPoints,
            awardedAt = evt.AwardedAt
        };

        return Task.WhenAll(
            _hubContext.Clients.Group($"user_{evt.UserId}").SendAsync("ReceiveSkillPointsAwarded", evt, cancellationToken),
            _hubContext.Clients.All.SendAsync("LeaderboardUpdated", payload, cancellationToken));
    }

    private static bool IsCommunity(NotificationCreatedEvent evt)
    {
        if (string.Equals(evt.Category, "community", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return CommunityTypes.Contains(evt.Type);
    }

    // Push new chat message notification (full info) to user's groups using dedicated channel
    public Task PushChatMessageNotificationAsync(NewMessageNotificationEvent evt, CancellationToken cancellationToken = default)
        => _hubContext.Clients.Group($"user_{evt.ToUserId}").SendAsync("ReceiveChatNotification", evt, cancellationToken);

    // Backwards-compatible alias
    public Task PushNewMessageNotificationAsync(NewMessageNotificationEvent evt, CancellationToken cancellationToken = default)
        => PushChatMessageNotificationAsync(evt, cancellationToken);
}


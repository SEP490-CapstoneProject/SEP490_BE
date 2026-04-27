using RecruitmentPlatform.Contracts.Realtime;

namespace Realtime.Application.Interfaces;

public interface IRealtimePushService
{
    Task PushNotificationAsync(NotificationCreatedEvent evt, CancellationToken cancellationToken = default);
    Task PushCommunityNotificationAsync(NotificationCreatedEvent evt, CancellationToken cancellationToken = default);
    Task PushSystemNotificationAsync(NotificationCreatedEvent evt, CancellationToken cancellationToken = default);
    Task PushCommentAsync(CommentCreatedEvent evt, CancellationToken cancellationToken = default);
    Task PushReplyAsync(ReplyCreatedEvent evt, CancellationToken cancellationToken = default);
    Task PushPostFavoriteChangedAsync(PostFavoriteChangedEvent evt, CancellationToken cancellationToken = default);
    Task PushConnectionRequestedAsync(ConnectionRequestedEvent evt, CancellationToken cancellationToken = default);
    Task PushConnectionAcceptedAsync(ConnectionAcceptedEvent evt, CancellationToken cancellationToken = default);
    /// <summary>
    /// Push tổng số tin nhắn mới (gom từ tất cả các room) cho user.
    /// </summary>
    Task PushNewMessageNotificationAsync(int toUserId, int totalNewMessages, CancellationToken cancellationToken = default);
}

using RecruitmentPlatform.Contracts.Realtime;

namespace Realtime.Application.Interfaces;

public interface IRealtimePushService
{
    Task PushNotificationAsync(NotificationCreatedEvent evt, CancellationToken cancellationToken = default);
    Task PushCommentAsync(CommentCreatedEvent evt, CancellationToken cancellationToken = default);
    Task PushReplyAsync(ReplyCreatedEvent evt, CancellationToken cancellationToken = default);
}

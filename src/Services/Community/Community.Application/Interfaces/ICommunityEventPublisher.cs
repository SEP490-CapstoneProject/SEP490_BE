using RecruitmentPlatform.Contracts.Realtime;

namespace Community.Application.Interfaces;

public interface ICommunityEventPublisher
{
    Task PublishCommentCreatedAsync(CommentCreatedEvent evt, CancellationToken cancellationToken = default);
    Task PublishReplyCreatedAsync(ReplyCreatedEvent evt, CancellationToken cancellationToken = default);
    Task PublishPostFavoriteChangedAsync(PostFavoriteChangedEvent evt, CancellationToken cancellationToken = default);
    Task PublishPostModerationEventAsync(PostModerationEvent evt, CancellationToken cancellationToken = default);
}

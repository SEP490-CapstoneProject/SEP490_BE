using Community.Application.Models.Events;

namespace Community.Application.Interfaces;

public interface INotificationEventPublisher
{
    Task PublishPostFavoriteNotificationAsync(PostFavoriteNotificationEvent evt, CancellationToken cancellationToken = default);
    Task PublishPostRemovedByModerationNotificationAsync(PostRemovedByModerationNotificationEvent evt, CancellationToken cancellationToken = default);
    Task PublishPostReportCreatedNotificationAsync(PostReportCreatedNotificationEvent evt, CancellationToken cancellationToken = default);
    Task PublishPostApprovedNotificationAsync(PostApprovedNotificationEvent evt, CancellationToken cancellationToken = default);
    Task PublishPostRejectedNotificationAsync(PostRejectedNotificationEvent evt, CancellationToken cancellationToken = default);
    Task PublishPostPendingReviewNotificationAsync(PostPendingReviewNotificationEvent evt, CancellationToken cancellationToken = default);
}

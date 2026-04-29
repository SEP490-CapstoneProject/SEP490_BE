using Company.Application.Models.Events;

namespace Company.Application.Interfaces;

public interface ICompanyNotificationEventPublisher
{
    Task PublishPostApprovedNotificationAsync(PostApprovedNotificationEvent evt, CancellationToken cancellationToken = default);
    Task PublishPostRejectedNotificationAsync(PostRejectedNotificationEvent evt, CancellationToken cancellationToken = default);
    Task PublishPostPendingReviewNotificationAsync(PostPendingReviewNotificationEvent evt, CancellationToken cancellationToken = default);
}

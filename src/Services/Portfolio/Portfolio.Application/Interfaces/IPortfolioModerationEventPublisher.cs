using Portfolio.Application.Models.Events;

namespace Portfolio.Application.Interfaces;

public interface IPortfolioModerationEventPublisher
{
    Task PublishPortfolioApprovedNotificationAsync(PortfolioApprovedNotificationEvent evt, CancellationToken cancellationToken = default);
    Task PublishPortfolioRejectedNotificationAsync(PortfolioRejectedNotificationEvent evt, CancellationToken cancellationToken = default);
    Task PublishPortfolioPendingReviewNotificationAsync(PortfolioPendingReviewNotificationEvent evt, CancellationToken cancellationToken = default);
    Task PublishPortfolioModerationEventAsync(object evt, CancellationToken cancellationToken = default);
}

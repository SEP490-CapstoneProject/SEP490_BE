using Portfolio.Application.Models.Events;

namespace Portfolio.Application.Interfaces;

public interface IPortfolioNotificationEventPublisher
{
    Task PublishComplimentCreatedAsync(PortfolioNotificationEventPayload payload, CancellationToken cancellationToken = default);
    Task PublishReportCreatedAsync(PortfolioReportCreatedNotificationEvent evt, CancellationToken cancellationToken = default);
}

public class PortfolioNotificationEventPayload
{
    public string EventId { get; set; } = Guid.NewGuid().ToString("N");
    public string EventType { get; set; } = "portfolio.compliment.created";
    public int Version { get; set; } = 1;
    public string UserId { get; set; } = string.Empty;
    public string? ActorId { get; set; }
    public string ActorType { get; set; } = "USER";
    public string? ObjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = "PORTFOLIO_REVIEWED";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

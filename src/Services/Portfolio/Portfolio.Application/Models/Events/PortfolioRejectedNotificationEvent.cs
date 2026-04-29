using RecruitmentPlatform.Contracts.Time;

namespace Portfolio.Application.Models.Events;

public sealed class PortfolioRejectedNotificationEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString("N");
    public string EventType { get; set; } = "portfolio.rejected";
    public int Version { get; set; } = 1;
    public string UserId { get; set; } = string.Empty;
    public string ActorId { get; set; } = "SYSTEM";
    public string ActorType { get; set; } = "SYSTEM";
    public string? ObjectId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = "PORTFOLIO_REJECTED";
    public DateTime CreatedAt { get; set; } = VietnamTime.Now();
}

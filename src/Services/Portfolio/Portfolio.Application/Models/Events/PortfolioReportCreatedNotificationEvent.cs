using RecruitmentPlatform.Contracts.Time;

namespace Portfolio.Application.Models.Events;

public sealed class PortfolioReportCreatedNotificationEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString("N");
    public string EventType { get; set; } = "portfolio.reported";
    public int Version { get; set; } = 1;
    public int PortfolioId { get; set; }
    public int ReporterUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = "PORTFOLIO_REPORT_REVIEW";
    public string[] TargetRoles { get; set; } = new[] { "ADMIN", "MODERATOR" };
    public DateTime CreatedAt { get; set; } = VietnamTime.Now();
}

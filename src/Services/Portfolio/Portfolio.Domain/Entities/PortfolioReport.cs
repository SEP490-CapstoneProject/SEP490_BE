using Portfolio.Domain.Enums;
using RecruitmentPlatform.Common;

namespace Portfolio.Domain.Entities;

public class PortfolioReport : BaseEntity
{
    public int PortfolioId { get; set; }
    public int ReporterUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PortfolioReportStatus Status { get; set; } = PortfolioReportStatus.Pending;
    public int? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }

    public Portfolio Portfolio { get; set; } = null!;
}

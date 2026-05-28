using Company.Domain.Enums;
using RecruitmentPlatform.Common;

namespace Company.Domain.Entities;

public class CompanyPostReport : BaseEntity
{
    public int CompanyPostId { get; set; }
    public int ReporterUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PostReportStatus Status { get; set; } = PostReportStatus.Pending;
    public int? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }

    public CompanyPost CompanyPost { get; set; } = null!;
}

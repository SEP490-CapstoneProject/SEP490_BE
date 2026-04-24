using Community.Domain.Enums;
using RecruitmentPlatform.Common;

namespace Community.Domain.Entities;

public class CommunityPostReport : BaseEntity
{
    public int CommunityPostId { get; set; }
    public int ReporterUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PostReportStatus Status { get; set; } = PostReportStatus.Pending;
    public int? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }

    public CommunityPost CommunityPost { get; set; } = null!;
}

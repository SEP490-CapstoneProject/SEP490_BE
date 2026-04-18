using Community.Application.Helpers;

namespace Community.Application.Models.Events;

public sealed class PostReportCreatedNotificationEvent
{
    public string EventId { get; set; } = Guid.NewGuid().ToString("N");
    public string EventType { get; set; } = "post.report.created";
    public int Version { get; set; } = 1;
    public string UserId { get; set; } = string.Empty; // role-targeted event, recipient resolved downstream
    public string? ActorId { get; set; } // Reporter user id
    public string ActorType { get; set; } = "USER";
    public string? ObjectId { get; set; } // PostId
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Type { get; set; } = "COMMUNITY_REPORT_REVIEW";
    public DateTime CreatedAt { get; set; } = DateTimeHelper.GetVietnamTime();
    public int PostId { get; set; }
    public int ReportId { get; set; }
    public int ReporterUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string[] TargetRoles { get; set; } = ["ADMIN", "MODERATOR"];
}

namespace Community.Application.DTOs;

public class CreatePostReportRequest
{
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class ReviewPostReportRequest
{
    public string Action { get; set; } = string.Empty; // approve_violation | reject
    public string? ReviewNote { get; set; }
}

public class AdminPostReportFilter
{
    public int? PostId { get; set; }
    public int? ReporterUserId { get; set; }
    public string? Status { get; set; } // Pending | Approved | Rejected
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class AdminPostFilter
{
    public string? Status { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class CommunityPostReportDto
{
    public int Id { get; set; }
    public int CommunityPostId { get; set; }
    public int PostOwnerUserId { get; set; }
    public int ReporterUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

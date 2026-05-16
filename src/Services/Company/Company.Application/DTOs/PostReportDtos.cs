namespace Company.Application.DTOs;

public class CreatePostReportRequest
{
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class CompanyPostReportDto
{
    public int Id { get; set; }
    public int CompanyPostId { get; set; }
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

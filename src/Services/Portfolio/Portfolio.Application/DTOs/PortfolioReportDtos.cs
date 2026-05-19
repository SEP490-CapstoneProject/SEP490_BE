namespace Portfolio.Application.DTOs;

public class CreatePortfolioReportRequest
{
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class ReviewPortfolioReportRequest
{
    public string Action { get; set; } = string.Empty; // approve_violation | reject
    public string? ReviewNote { get; set; }
}

public class PortfolioReportDto
{
    public int Id { get; set; }
    public int PortfolioId { get; set; }
    public int ReporterUserId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Status { get; set; }
    public int? ReviewedByUserId { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewNote { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

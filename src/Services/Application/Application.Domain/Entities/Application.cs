namespace Application.Domain.Entities;

public class Application
{
    public int ApplicationId { get; set; }
    public int EmployeeId { get; set; }
    public int CompanyId { get; set; }
    public int CompanyPostId { get; set; }
    public int PortfolioId { get; set; }
    public int? RoomId { get; set; }
    public ApplicationStatus Status { get; set; } = ApplicationStatus.WAITING;
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

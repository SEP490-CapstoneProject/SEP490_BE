namespace Portfolio.Domain.Entities;

public enum ComplimentState
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

public class Compliment
{
    public int Id { get; set; }
    public int PortfolioId { get; set; }
    public int CompanyId { get; set; }
    public string Content { get; set; } = string.Empty;
    public int? Score { get; set; }
    public ComplimentState State { get; set; } = ComplimentState.Pending;
    public bool IsDeleted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    public Portfolio Portfolio { get; set; } = null!;
}

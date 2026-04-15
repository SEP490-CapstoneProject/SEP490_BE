namespace Portfolio.Domain.Entities;

public enum ComplimentState
{
    Pending = 0,
    Approved = 1,
    Rejected = 2,
    Deleted = 3
}

public class Compliment
{
    public int Id { get; set; }
    public int PortfolioId { get; set; }
    public int UserId { get; set; }
    public string? Content { get; set; }
    public int? Score { get; set; }
    public ComplimentState State { get; set; } = ComplimentState.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedBy { get; set; }

    public Portfolio Portfolio { get; set; } = null!;
}

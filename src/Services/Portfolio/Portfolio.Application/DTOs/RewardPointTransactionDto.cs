namespace Portfolio.Application.DTOs;

public class RewardPointTransactionDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal Points { get; set; }
    public string Type { get; set; } = string.Empty; // "Earn" or "Spend"
    public string SourceType { get; set; } = string.Empty;
    public string SourceId { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PointBalanceDto
{
    public int UserId { get; set; }
    public decimal CurrentBalance { get; set; }
    public int TodayEarned { get; set; }
    public decimal TotalEarned { get; set; }
    public decimal TotalSpent { get; set; }
    public DateTime? LastTransactionAt { get; set; }
}

public class RedeemPointsRequest
{
    public decimal Points { get; set; }
    public string SourceType { get; set; } = string.Empty; // e.g., "SponsoredFeedRedemption"
    public string? Description { get; set; }
}

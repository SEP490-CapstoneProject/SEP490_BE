namespace Portfolio.Domain.Entities;

public enum RewardPointType
{
    Earn = 0,
    Spend = 1
}

public enum RewardPointSourceType
{
    ComplimentReview = 0,
    SponsoredFeedRedemption = 1,
    AdminAdjustment = 2
}

/// <summary>
/// Tracks all reward point transactions for recruiters.
/// Points are earned through compliments and spent on sponsored feed.
/// </summary>
public class RewardPointTransaction
{
    public int Id { get; set; }
    
    /// <summary>
    /// Recruiter/Company UserId who earned or spent points
    /// </summary>
    public int UserId { get; set; }
    
    /// <summary>
    /// Number of points earned or spent
    /// </summary>
    public decimal Points { get; set; }
    
    /// <summary>
    /// Type of transaction: Earn or Spend
    /// </summary>
    public RewardPointType Type { get; set; }
    
    /// <summary>
    /// Source type: ComplimentReview, SponsoredFeedRedemption, AdminAdjustment
    /// </summary>
    public RewardPointSourceType SourceType { get; set; }
    
    /// <summary>
    /// ID of the source (e.g., ComplimentId, SponsoredContentId)
    /// </summary>
    public string SourceId { get; set; } = string.Empty;
    
    /// <summary>
    /// Optional description for the transaction
    /// </summary>
    public string? Description { get; set; }
    
    /// <summary>
    /// When the transaction occurred
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

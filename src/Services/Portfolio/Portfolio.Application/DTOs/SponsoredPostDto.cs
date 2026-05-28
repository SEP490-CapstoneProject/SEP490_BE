namespace Portfolio.Application.DTOs;

public class CreateSponsoredPostRequest
{
    /// <summary>
    /// Type of content: "Text", "Image", "Video"
    /// </summary>
    public string ContentType { get; set; } = string.Empty;
    
    /// <summary>
    /// Text content (required for Text type, optional caption for Image/Video)
    /// </summary>
    public string? TextContent { get; set; }
    
    /// <summary>
    /// URL to image (required for Image type)
    /// </summary>
    public string? ImageUrl { get; set; }
    
    /// <summary>
    /// URL to video (required for Video type)
    /// </summary>
    public string? VideoUrl { get; set; }
    
    /// <summary>
    /// Number of points to spend (5, 12, or custom amount)
    /// </summary>
    public decimal PointsToSpend { get; set; }
    
    /// <summary>
    /// Optional landing page URL
    /// </summary>
    public string? ClickThroughUrl { get; set; }
}

public class SponsoredPostDto
{
    public int Id { get; set; }
    public int CreatedBy { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public string? TextContent { get; set; }
    public string? ImageUrl { get; set; }
    public string? VideoUrl { get; set; }
    public decimal PointsSpent { get; set; }
    public int DurationDays { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime ExpiryDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ClickThroughUrl { get; set; }
    public int ViewCount { get; set; }
    public int ClickCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class PointsBalanceDto
{
    public int UserId { get; set; }
    public decimal CurrentBalance { get; set; }
    public int TodayEarned { get; set; }
    public decimal TotalEarned { get; set; }
    public decimal TotalSpent { get; set; }
    public DateTime? LastTransactionAt { get; set; }
}

public class PointTransactionHistoryDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal Points { get; set; }
    public string Type { get; set; } = string.Empty; // Earn or Spend
    public string SourceType { get; set; } = string.Empty;
    public string SourceId { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
}

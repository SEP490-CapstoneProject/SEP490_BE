namespace Portfolio.Domain.Entities;

/// <summary>
/// Type of content in a sponsored post: text only, image, or video
/// </summary>
public enum SponsoredContentType
{
    Text = 0,
    Image = 1,
    Video = 2
}

/// <summary>
/// Status of a sponsored post
/// </summary>
public enum SponsoredPostStatus
{
    Active = 0,
    Expired = 1,
    Paused = 2,
    Deleted = 3
}

/// <summary>
/// Represents a sponsored post created by a recruiter using reward points.
/// Recruiters can create posts with text, image, or video content.
/// Duration is determined by the points spent (5pts=1day, 12pts=3days, etc.).
/// </summary>
public class SponsoredPost
{
    public int Id { get; set; }
    
    /// <summary>
    /// Recruiter/Company who created this sponsored post
    /// </summary>
    public int CreatedBy { get; set; }
    
    /// <summary>
    /// Type of content: Text, Image, or Video
    /// </summary>
    public SponsoredContentType ContentType { get; set; }
    
    /// <summary>
    /// Text content (required for Text type, optional caption for Image/Video)
    /// </summary>
    public string? TextContent { get; set; }
    
    /// <summary>
    /// URL to image content (for Image type)
    /// </summary>
    public string? ImageUrl { get; set; }
    
    /// <summary>
    /// URL to video content (for Video type)
    /// </summary>
    public string? VideoUrl { get; set; }
    
    /// <summary>
    /// Number of points spent to create this post
    /// </summary>
    public decimal PointsSpent { get; set; }
    
    /// <summary>
    /// Number of days this post will be visible
    /// </summary>
    public int DurationDays { get; set; }
    
    /// <summary>
    /// When the post starts being visible
    /// </summary>
    public DateTime StartDate { get; set; }
    
    /// <summary>
    /// When the post expires (StartDate + DurationDays)
    /// </summary>
    public DateTime ExpiryDate { get; set; }
    
    /// <summary>
    /// Current status: Active, Expired, Paused, Deleted
    /// </summary>
    public SponsoredPostStatus Status { get; set; } = SponsoredPostStatus.Active;
    
    /// <summary>
    /// Click-through URL (optional landing page)
    /// </summary>
    public string? ClickThroughUrl { get; set; }
    
    /// <summary>
    /// Number of times this post has been viewed
    /// </summary>
    public int ViewCount { get; set; } = 0;
    
    /// <summary>
    /// Number of clicks on this post
    /// </summary>
    public int ClickCount { get; set; } = 0;
    
    /// <summary>
    /// When the post was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// When the post was last updated
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
    
    /// <summary>
    /// Priority score for ranking in feeds (0-100, default 50)
    /// </summary>
    public decimal? PriorityScore { get; set; } = 50m;
    
    /// <summary>
    /// Maximum impressions allowed for this sponsored post
    /// </summary>
    public int MaxImpression { get; set; } = 1000;
    
    /// <summary>
    /// Current impression count
    /// </summary>
    public int CurrentImpression { get; set; } = 0;
    
    /// <summary>
    /// Maximum clicks allowed for this sponsored post
    /// </summary>
    public int MaxClick { get; set; } = 100;
    
    /// <summary>
    /// Current click count (tracked separately for analytics)
    /// </summary>
    public int CurrentClick { get; set; } = 0;
}

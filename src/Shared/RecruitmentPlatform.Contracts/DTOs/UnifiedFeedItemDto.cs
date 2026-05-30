namespace RecruitmentPlatform.Contracts.DTOs;

/// <summary>
/// Unified DTO for all feed item types: Portfolio, CompanyPost, CommunityPost, SponsoredPost.
/// Allows mixed feed presentation with natural sponsored post injection across all services.
/// </summary>
public class UnifiedFeedItemDto
{
    /// <summary>
    /// Type of item: "Portfolio", "CompanyPost", "CommunityPost", "SponsoredPost"
    /// </summary>
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// Whether this item is a sponsored post
    /// </summary>
    public bool IsSponsored { get; set; }
    
    /// <summary>
    /// Label to display for sponsored items (e.g., "Sponsored")
    /// </summary>
    public string? SponsoredLabel { get; set; }
    
    /// <summary>
    /// The actual item data: PortfolioDto, CompanyPostDto, CommunityPostDto, or SponsoredPostDto
    /// </summary>
    public object? Data { get; set; }
}

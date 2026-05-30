using Portfolio.Domain.Entities;

namespace Portfolio.Application.Services;

/// <summary>
/// Ranks sponsored posts for feed injection based on priority, remaining impressions, and CTR.
/// Score = (priority × 0.5) + (remainingRatio × 0.3) + (ctr × 0.2)
/// </summary>
public class RankingEngine
{
    /// <summary>
    /// Calculate ranking score for a sponsored post.
    /// </summary>
    public decimal CalculateScore(SponsoredPost post)
    {
        // Priority score: weighted 50%
        decimal priorityScore = post.PriorityScore ?? 50m;
        if (priorityScore > 100) priorityScore = 100;
        if (priorityScore < 0) priorityScore = 0;
        
        // Remaining impression ratio: weighted 30%
        decimal maxImpression = post.MaxImpression > 0 ? post.MaxImpression : 1000m;
        decimal remainingRatio = (maxImpression - post.CurrentImpression) / maxImpression;
        if (remainingRatio < 0) remainingRatio = 0;
        if (remainingRatio > 1) remainingRatio = 1;
        
        // Click-through rate (CTR): weighted 20%
        decimal views = post.ViewCount > 0 ? post.ViewCount : 1;
        decimal clicks = post.ClickCount;
        decimal ctr = clicks / views;
        
        // Composite score
        return (priorityScore * 0.5m) + (remainingRatio * 0.3m) + (ctr * 0.2m);
    }
    
    /// <summary>
    /// Rank sponsored posts by score in descending order.
    /// </summary>
    public List<SponsoredPost> RankSponsoredPosts(List<SponsoredPost> posts)
    {
        return posts
            .OrderByDescending(p => CalculateScore(p))
            .ToList();
    }
}

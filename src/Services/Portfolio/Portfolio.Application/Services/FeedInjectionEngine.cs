using Microsoft.Extensions.Caching.Memory;
using Portfolio.Application.DTOs;
using Portfolio.Domain.Entities;

namespace Portfolio.Application.Services;

/// <summary>
/// Injects sponsored posts naturally into feed streams with frequency capping.
/// Rules:
/// - Inject 1 sponsored post every 5-10 normal posts (random)
/// - Never place 2 sponsored posts consecutively
/// - Respect frequency cap: max 3 impressions/day/user per sponsored post
/// </summary>
public class FeedInjectionEngine
{
    private readonly RankingEngine _rankingEngine;
    private readonly IMemoryCache _cache;
    private readonly Random _random = new Random();
    
    public FeedInjectionEngine(RankingEngine rankingEngine, IMemoryCache cache)
    {
        _rankingEngine = rankingEngine;
        _cache = cache;
    }
    
    /// <summary>
    /// Merge normal feed items with ranked sponsored posts using randomized injection.
    /// </summary>
    public List<UnifiedFeedItemDto> InjectSponsoredPosts(
        List<UnifiedFeedItemDto> normalItems,
        List<SponsoredPost> sponsoredPosts,
        int userId)
    {
        // Edge cases
        if (!sponsoredPosts.Any() || !normalItems.Any())
            return normalItems;
        
        // Filter by active status and frequency cap
        var eligibleSponsored = FilterByFrequencyCap(sponsoredPosts, userId);
        if (!eligibleSponsored.Any())
            return normalItems;
        
        // Rank sponsored posts
        var rankedSponsored = _rankingEngine.RankSponsoredPosts(eligibleSponsored);
        var sponsoredQueue = new Queue<SponsoredPost>(rankedSponsored);
        
        var result = new List<UnifiedFeedItemDto>();
        int nextInjectPosition = _random.Next(5, 11);  // First inject at position 5-10
        
        for (int i = 0; i < normalItems.Count; i++)
        {
            // Add normal item
            result.Add(normalItems[i]);
            
            // Check if we should inject a sponsored post
            if (sponsoredQueue.Count > 0 
                && i + 1 >= nextInjectPosition
                && !IsLastItemSponsored(result))
            {
                var sponsoredPost = sponsoredQueue.Dequeue();
                
                var sponsoredItem = new UnifiedFeedItemDto
                {
                    Type = "SponsoredPost",
                    IsSponsored = true,
                    SponsoredLabel = "Sponsored",
                    Data = MapToDto(sponsoredPost)
                };
                result.Add(sponsoredItem);
                
                // Increment impression count and cache
                IncrementFrequencyCap(sponsoredPost.Id, userId);
                
                // Next injection: another 5-10 posts away
                nextInjectPosition = i + 1 + _random.Next(5, 11);
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Filter sponsored posts by:
    /// 1. Active status only
    /// 2. Frequency cap not exceeded (max 3 impressions/day/user)
    /// </summary>
    private List<SponsoredPost> FilterByFrequencyCap(List<SponsoredPost> posts, int userId)
    {
        return posts
            .Where(p => p.Status == SponsoredPostStatus.Active)
            .Where(p => GetFrequencyCapCount(p.Id, userId) < 3)
            .ToList();
    }
    
    /// <summary>
    /// Get current impression count for user on this sponsored post today.
    /// </summary>
    private int GetFrequencyCapCount(int sponsoredId, int userId)
    {
        string key = $"sponsor_impression_{userId}_{sponsoredId}_{DateTime.UtcNow:yyyy-MM-dd}";
        if (_cache.TryGetValue(key, out int count))
        {
            return count;
        }
        return 0;
    }
    
    /// <summary>
    /// Increment impression count for user on this sponsored post.
    /// </summary>
    private void IncrementFrequencyCap(int sponsoredId, int userId)
    {
        string key = $"sponsor_impression_{userId}_{sponsoredId}_{DateTime.UtcNow:yyyy-MM-dd}";
        int count = GetFrequencyCapCount(sponsoredId, userId);
        count++;
        
        // Cache for 24 hours
        var cacheOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
        };
        _cache.Set(key, count, cacheOptions);
    }
    
    /// <summary>
    /// Check if the last item in result is a sponsored post (prevent consecutive sponsors).
    /// </summary>
    private bool IsLastItemSponsored(List<UnifiedFeedItemDto> items)
    {
        return items.Count > 0 && items[^1].IsSponsored;
    }
    
    /// <summary>
    /// Map SponsoredPost entity to DTO for feed display.
    /// </summary>
    private SponsoredPostDto MapToDto(SponsoredPost post)
    {
        return new SponsoredPostDto
        {
            Id = post.Id,
            CreatedBy = post.CreatedBy,
            ContentType = post.ContentType.ToString(),
            TextContent = post.TextContent,
            ImageUrl = post.ImageUrl,
            VideoUrl = post.VideoUrl,
            PointsSpent = post.PointsSpent,
            DurationDays = post.DurationDays,
            StartDate = post.StartDate,
            ExpiryDate = post.ExpiryDate,
            Status = post.Status.ToString(),
            ClickThroughUrl = post.ClickThroughUrl,
            ViewCount = post.ViewCount,
            ClickCount = post.ClickCount,
            CreatedAt = post.CreatedAt,
            UpdatedAt = post.UpdatedAt
        };
    }
}

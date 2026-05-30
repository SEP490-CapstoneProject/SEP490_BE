using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Portfolio.Application.DTOs;
using Portfolio.Domain.Entities;
using RecruitmentPlatform.Contracts.DTOs;

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
    private readonly ILogger<FeedInjectionEngine> _logger;
    private readonly Random _random = new Random();
    
    public FeedInjectionEngine(RankingEngine rankingEngine, IMemoryCache cache, ILogger<FeedInjectionEngine> logger)
    {
        _rankingEngine = rankingEngine;
        _cache = cache;
        _logger = logger;
    }
    
    /// <summary>
    /// Merge normal feed items with ranked sponsored posts using randomized injection.
    /// </summary>
    public List<UnifiedFeedItemDto> InjectSponsoredPosts(
        List<UnifiedFeedItemDto> normalItems,
        List<SponsoredPost> sponsoredPosts,
        int userId)
    {
        _logger.LogInformation("InjectSponsoredPosts called: normalItems={NormalCount}, sponsoredPosts={SponsoredCount}, userId={UserId}", normalItems?.Count ?? 0, sponsoredPosts?.Count ?? 0, userId);

        // Edge cases
        if (!sponsoredPosts.Any() || !normalItems.Any())
        {
            _logger.LogInformation("No sponsored or no normal items - returning normal feed (normal={NormalCount}, sponsored={SponsoredCount})", normalItems?.Count ?? 0, sponsoredPosts?.Count ?? 0);
            return normalItems;
        }
        
        // Filter by active status and frequency cap
        var eligibleSponsored = FilterByFrequencyCap(sponsoredPosts, userId);
        _logger.LogInformation("Eligible sponsored posts after frequency cap: {Count}", eligibleSponsored.Count);
        if (eligibleSponsored.Any())
        {
            _logger.LogDebug("Eligible sponsored ids: {Ids}", string.Join(',', eligibleSponsored.Select(p => p.Id)));
            foreach (var p in eligibleSponsored)
            {
                _logger.LogDebug("SponsoredId={Id} CurrentImpression={CurrentImpression} CurrentClick={CurrentClick} ViewCount={ViewCount} ClickCount={ClickCount}", p.Id, p.CurrentImpression, p.CurrentClick, p.ViewCount, p.ClickCount);
            }
        }
        else
        {
            _logger.LogInformation("No eligible sponsored posts after applying frequency cap - returning normal feed");
            return normalItems;
        }
        
        // Rank sponsored posts
        var rankedSponsored = _rankingEngine.RankSponsoredPosts(eligibleSponsored);
        _logger.LogInformation("Ranked sponsored order: {Ids}", string.Join(',', rankedSponsored.Select(p => p.Id)));
        var sponsoredQueue = new Queue<SponsoredPost>(rankedSponsored);
        
        var result = new List<UnifiedFeedItemDto>();
        int nextInjectPosition = _random.Next(5, 11);  // First inject at position 5-10
        _logger.LogInformation("Initial nextInjectPosition={Pos}", nextInjectPosition);
        
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
                _logger.LogInformation("Injecting sponsored post id={Id} at feed index={Index}", sponsoredPost.Id, i);
                
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
                // Track this id in per-user in-memory seen set so subsequent calls won't return the same id immediately
                try
                {
                    AddToUserSeenSet(userId, sponsoredPost.Id);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to add sponsored id {Id} to user seen set", sponsoredPost.Id);
                }
                
                // Next injection: another 5-10 posts away
                nextInjectPosition = i + 1 + _random.Next(5, 11);
                _logger.LogInformation("Next injection scheduled at position={Pos}", nextInjectPosition);
            }
        }
        
        return result;
    }
    
    /// <summary>
    /// Filter sponsored posts by:
    /// 1. Active status only
    /// 2. Frequency cap not exceeded (max 3 impressions/day/user)
    /// 3. Exclude posts the user has recently seen (in-memory per-user cache)
    /// </summary>
    private List<SponsoredPost> FilterByFrequencyCap(List<SponsoredPost> posts, int userId)
    {
        // Get per-user seen set from cache (in-memory, TTL)
        var seen = GetUserSeenSet(userId);

        var filtered = posts
            .Where(p => p.Status == SponsoredPostStatus.Active)
            .Where(p => GetFrequencyCapCount(p.Id, userId) < 3)
            .Where(p => !seen.Contains(p.Id))
            .ToList();
        _logger.LogDebug("FilterByFrequencyCap: postsBefore={Before}, postsAfter={After} seenCount={SeenCount}", posts.Count, filtered.Count, seen.Count);
        return filtered;
    }
    
    /// <summary>
    /// Get or create in-memory HashSet of recently seen sponsored ids for this user.
    /// Uses a TTL so this state is short-lived and does not require DB changes.
    /// Anonymous users share a common key (may cause collision) — consider longer term Redis-per-user solution.
    /// </summary>
    private HashSet<int> GetUserSeenSet(int userId)
    {
        var key = userId > 0 ? $"sponsored_seen_{userId}" : "sponsored_seen_anonymous";
        if (_cache.TryGetValue(key, out HashSet<int>? set) && set != null)
        {
            return set;
        }
        // Create new set with TTL (1 hour)
        set = new HashSet<int>();
        var cacheOptions = new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) };
        _cache.Set(key, set, cacheOptions);
        return set;
    }

    /// <summary>
    /// Add sponsored id to user's seen set and refresh TTL.
    /// </summary>
    private void AddToUserSeenSet(int userId, int sponsoredId)
    {
        var key = userId > 0 ? $"sponsored_seen_{userId}" : "sponsored_seen_anonymous";
        var set = GetUserSeenSet(userId);
        lock (set)
        {
            set.Add(sponsoredId);
        }
        // Refresh TTL by re-setting with same options
        var cacheOptions = new MemoryCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1) };
        _cache.Set(key, set, cacheOptions);
    }

    /// <summary>
    /// Get current impression count for user on this sponsored post today.
    /// </summary>
    private int GetFrequencyCapCount(int sponsoredId, int userId)
    {
        string key = $"sponsor_impression_{userId}_{sponsoredId}_{DateTime.UtcNow:yyyy-MM-dd}";
        if (_cache.TryGetValue(key, out int count))
        {
            _logger.LogDebug("GetFrequencyCapCount: key={Key} count={Count}", key, count);
            return count;
        }
        _logger.LogDebug("GetFrequencyCapCount: key={Key} not found, returning 0", key);
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
        _logger.LogInformation("IncrementFrequencyCap: key={Key} newCount={Count}", key, count);
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
    public List<SponsoredPost> GetEligibleAndRankedSponsoredPosts(List<SponsoredPost> posts, int userId)
    {
        var eligible = FilterByFrequencyCap(posts, userId);
        var ranked = _rankingEngine.RankSponsoredPosts(eligible);
        _logger.LogInformation("GetEligibleAndRankedSponsoredPosts: userId={UserId}, eligibleCount={Count}", userId, ranked.Count);
        return ranked;
    }

    public void MarkSponsoredIdsSeen(int userId, IEnumerable<int> ids)
    {
        foreach (var id in ids)
        {
            try
            {
                AddToUserSeenSet(userId, id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "MarkSponsoredIdsSeen failed for id={Id}", id);
            }
        }
    }

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

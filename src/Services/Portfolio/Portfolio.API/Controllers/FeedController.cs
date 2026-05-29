using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;
using Portfolio.Application.Services;
using Portfolio.Domain.Entities;

namespace Portfolio.API.Controllers;

[ApiController]
[Route("api/feed")]
public class FeedController : ControllerBase
{
    private readonly IPortfolioService _portfolioService;
    private readonly IBlockRepository _blockRepo;
    private readonly BlockService _blockService;
    private readonly ISponsoredPostRepository _sponsoredPostRepository;
    private readonly FeedInjectionEngine _feedInjectionEngine;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<FeedController> _logger;

    public FeedController(
        IPortfolioService portfolioService,
        IBlockRepository blockRepo,
        BlockService blockService,
        ISponsoredPostRepository sponsoredPostRepository,
        FeedInjectionEngine feedInjectionEngine,
        ICurrentUserService currentUser,
        ILogger<FeedController> logger)
    {
        _portfolioService = portfolioService;
        _blockRepo = blockRepo;
        _blockService = blockService;
        _sponsoredPostRepository = sponsoredPostRepository;
        _feedInjectionEngine = feedInjectionEngine;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary>
    /// Get portfolio feed with injected sponsored posts.
    /// </summary>
    [HttpGet("portfolio")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPortfolioFeed(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        [FromQuery] string? q = null,
        [FromQuery] PortfolioSortMode sort = PortfolioSortMode.newest,
        [FromQuery] PortfolioRankBy rankBy = PortfolioRankBy.average)
    {
        try
        {
            // Fetch normal portfolio items
            var normalFeed = await _portfolioService.GetAllAsync(page, pageSize, status, q, null, sort, rankBy);

            // Enrich with blocks
            foreach (var p in normalFeed.Items)
            {
                var blocks = await _blockRepo.GetByPortfolioIdAsync(p.PortfolioId);
                p.Blocks = blocks.Select(b => _blockService.MapBlockToDto(b)).ToList();
            }

            // Map to UnifiedFeedItemDto
            var normalItems = normalFeed.Items
                .Select(p => new UnifiedFeedItemDto
                {
                    Type = "Portfolio",
                    IsSponsored = false,
                    Data = p
                })
                .ToList();

            // Fetch active sponsored posts (as entities) for injection
            var sponsoredPostEntities = await _sponsoredPostRepository.GetActivePostsAsync();

            // Inject sponsored posts with frequency capping
            var mixedFeed = _feedInjectionEngine.InjectSponsoredPosts(
                normalItems,
                sponsoredPostEntities,
                _currentUser.UserId
            );

            return Ok(new
            {
                items = mixedFeed,
                totalCount = normalFeed.Total,
                currentPage = page,
                pageSize = pageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching portfolio feed");
            return StatusCode(500, new { error = "Failed to fetch feed" });
        }
    }

    /// <summary>
    /// Get ranked and filtered sponsored posts for injection into other feeds (Company, Community).
    /// This is a service endpoint called by other services.
    /// </summary>
    [HttpGet("sponsored-posts/ranked")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRankedSponsoredPosts([FromQuery] int userId = 0)
    {
        try
        {
            var sponsoredPosts = await _sponsoredPostRepository.GetActivePostsAsync();

            if (!sponsoredPosts.Any())
                return Ok(new List<UnifiedFeedItemDto>());

            // Filter by frequency cap and rank
            var ranking = new RankingEngine();
            var ranked = ranking.RankSponsoredPosts(sponsoredPosts);

            var result = ranked.Select(s => new UnifiedFeedItemDto
            {
                Type = "SponsoredPost",
                IsSponsored = true,
                SponsoredLabel = "Sponsored",
                Data = MapToDto(s)
            }).ToList();

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching ranked sponsored posts");
            return StatusCode(500, new { error = "Failed to fetch sponsored posts" });
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

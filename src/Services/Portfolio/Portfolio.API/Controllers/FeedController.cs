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
    private readonly RankingEngine _rankingEngine;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<FeedController> _logger;

    public FeedController(
        IPortfolioService portfolioService,
        IBlockRepository blockRepo,
        BlockService blockService,
        ISponsoredPostRepository sponsoredPostRepository,
        FeedInjectionEngine feedInjectionEngine,
        RankingEngine rankingEngine,
        ICurrentUserService currentUser,
        ILogger<FeedController> logger)
    {
        _portfolioService = portfolioService;
        _blockRepo = blockRepo;
        _blockService = blockService;
        _sponsoredPostRepository = sponsoredPostRepository;
        _feedInjectionEngine = feedInjectionEngine;
        _rankingEngine = rankingEngine;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary>
    /// Get portfolio feed with injected sponsored posts.
    /// Supports same filters as GET /api/portfolio endpoint.
    /// </summary>
    [HttpGet("portfolio")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPortfolioFeed(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        [FromQuery] string? q = null,
        [FromQuery] string? blockType = null,
        [FromQuery] bool includeCompliments = false,
        [FromQuery] ComplimentState? complimentState = null,
        [FromQuery] bool? hasCompliment = null,
        [FromQuery] PortfolioRankBy rankBy = PortfolioRankBy.average,
        [FromQuery] PortfolioSortMode sort = PortfolioSortMode.newest)
    {
        try
        {
            // Use compliment filter path when any compliment param is specified
            if (includeCompliments || complimentState.HasValue || hasCompliment.HasValue)
            {
                var queryParams = new PortfolioQueryParams
                {
                    Page = page,
                    PageSize = pageSize,
                    Status = status,
                    SearchTerm = q,
                    BlockType = blockType,
                    IncludeCompliments = includeCompliments,
                    ComplimentState = complimentState,
                    HasCompliment = hasCompliment,
                    RankBy = rankBy,
                    Sort = sort
                };
                var filteredResult = await _portfolioService.GetAllWithComplimentFilterAsync(queryParams);

                foreach (var p in filteredResult.Items)
                {
                    var blocks = await _blockRepo.GetByPortfolioIdAsync(p.Id);
                    p.Blocks = blocks.Select(b => _blockService.MapBlockToDto(b)).ToList();
                }

                // Map to UnifiedFeedItemDto
                var complimentFeedItems = filteredResult.Items
                    .Select(p => new UnifiedFeedItemDto
                    {
                        Type = "Portfolio",
                        IsSponsored = false,
                        Data = p
                    })
                    .ToList();

                // Fetch active sponsored posts for injection
                var complimentSponsoredPosts = await _sponsoredPostRepository.GetActivePostsAsync();

                // Inject sponsored posts with frequency capping
                var complimentMixedFeed = _feedInjectionEngine.InjectSponsoredPosts(
                    complimentFeedItems,
                    complimentSponsoredPosts,
                    _currentUser.UserId
                );

                return Ok(new
                {
                    items = complimentMixedFeed,
                    totalCount = filteredResult.Total,
                    currentPage = page,
                    pageSize = pageSize
                });
            }

            // Standard portfolio feed without compliment filters
            var normalFeed = await _portfolioService.GetAllAsync(page, pageSize, status, q, blockType, sort, rankBy);

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
        catch (ArgumentException ex)
        {
            _logger.LogError(ex, "ArgumentException fetching portfolio feed: {Message}", ex.Message);
            return BadRequest(new { error = ex.Message });
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
            var ranked = _rankingEngine.RankSponsoredPosts(sponsoredPosts);

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

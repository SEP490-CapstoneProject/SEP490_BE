using Company.Application.DTOs;
using Company.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecruitmentPlatform.Contracts.DTOs;
using System.Security.Claims;
using System.Text.Json;

namespace Company.API.Controllers;

[ApiController]
[Route("api/company-posts")]
public class CompanyFeedController : ControllerBase
{
    private readonly ICompanyPostService _service;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<CompanyFeedController> _logger;

    public CompanyFeedController(
        ICompanyPostService service,
        IHttpClientFactory httpClientFactory,
        ILogger<CompanyFeedController> logger)
    {
        _service = service;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    private int? GetUserId()
    {
        var userIdRaw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst("nameid")?.Value
            ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
        return int.TryParse(userIdRaw, out var id) ? id : null;
    }

    /// <summary>Get company posts feed with injected sponsored posts (cursor-based pagination)</summary>
    [HttpGet("feed")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFeedWithSponsorship(
        [FromQuery] DateTime? cursor,
        [FromQuery] int limit = 10)
    {
        try
        {
            var userId = GetUserId();
            
            // 1. Fetch normal company posts
            var normalFeed = await _service.GetPostFeedAsync(cursor, limit, userId);

            // Map to UnifiedFeedItemDto
            var normalItems = normalFeed.Items
                .Select(p => new UnifiedFeedItemDto
                {
                    Type = "CompanyPost",
                    IsSponsored = false,
                    Data = p
                })
                .ToList();

            // 2. Fetch ranked sponsored posts from Portfolio service
            var sponsoredItems = await GetRankedSponsoredPostsAsync(userId);

            // 3. Inject sponsored posts into feed
            var mixedFeed = InjectSponsoredPosts(normalItems, sponsoredItems);

            return Ok(new
            {
                items = mixedFeed,
                cursor = normalFeed.NextCursor,
                limit = limit
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching company feed with sponsorship");
            return StatusCode(500, new { error = "Failed to fetch feed" });
        }
    }

    /// <summary>Get company posts from specific company with sponsored injection</summary>
    [HttpGet("company/{companyId:int}/feed")]
    [AllowAnonymous]
    public async Task<IActionResult> GetCompanyFeedWithSponsorship(
        int companyId,
        [FromQuery] DateTime? cursor,
        [FromQuery] int limit = 10)
    {
        try
        {
            var userId = GetUserId();

            // 1. Fetch company posts from specific company
            var normalFeed = await _service.GetPostsByCompanyAsync(companyId, cursor, limit, userId);

            // Map to UnifiedFeedItemDto
            var normalItems = normalFeed.Items
                .Select(p => new UnifiedFeedItemDto
                {
                    Type = "CompanyPost",
                    IsSponsored = false,
                    Data = p
                })
                .ToList();

            // 2. Fetch ranked sponsored posts
            var sponsoredItems = await GetRankedSponsoredPostsAsync(userId);

            // 3. Inject sponsored posts
            var mixedFeed = InjectSponsoredPosts(normalItems, sponsoredItems);

            return Ok(new
            {
                items = mixedFeed,
                cursor = normalFeed.NextCursor,
                limit = limit
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching company feed from company {CompanyId}: {Message}", companyId, ex.Message);
            return StatusCode(500, new { error = "Failed to fetch feed" });
        }
    }

    /// <summary>Fetch ranked sponsored posts from Portfolio service</summary>
    private async Task<List<UnifiedFeedItemDto>> GetRankedSponsoredPostsAsync(int? userId)
    {
        try
        {
            var httpClient = _httpClientFactory.CreateClient("PortfolioFeedClient");
            var userIdParam = userId.HasValue ? userId.Value : 0;
            var response = await httpClient.GetAsync($"/api/feed/sponsored-posts/ranked?userId={userIdParam}");

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Portfolio feed service returned {StatusCode}", response.StatusCode);
                return new List<UnifiedFeedItemDto>();
            }

            var content = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var items = JsonSerializer.Deserialize<List<UnifiedFeedItemDto>>(content, options) ?? new List<UnifiedFeedItemDto>();

            return items;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching sponsored posts from Portfolio service");
            return new List<UnifiedFeedItemDto>();
        }
    }

    /// <summary>Inject sponsored posts naturally into feed (randomized 5-10 item intervals)</summary>
    private List<UnifiedFeedItemDto> InjectSponsoredPosts(
        List<UnifiedFeedItemDto> normalItems,
        List<UnifiedFeedItemDto> sponsoredItems)
    {
        // Edge cases
        if (!sponsoredItems.Any() || !normalItems.Any())
            return normalItems;

        if (normalItems.Count < 5)
            return normalItems;  // Need at least 5 items to start injecting

        var result = new List<UnifiedFeedItemDto>();
        var random = new Random();
        var sponsoredQueue = new Queue<UnifiedFeedItemDto>(sponsoredItems);
        int nextInjectPosition = random.Next(5, 11);  // First inject at position 5-10

        for (int i = 0; i < normalItems.Count; i++)
        {
            result.Add(normalItems[i]);

            // Check if we should inject a sponsored post
            if (sponsoredQueue.Count > 0
                && i + 1 >= nextInjectPosition
                && (result.Count == 0 || result[result.Count - 2]?.IsSponsored == false))
            {
                result.Add(sponsoredQueue.Dequeue());
                nextInjectPosition = i + 1 + random.Next(5, 11);  // Next injection in 5-10 items
            }
        }

        // Add any remaining sponsored posts if space allows
        while (sponsoredQueue.Count > 0 && result.Count < 50)
        {
            result.Add(sponsoredQueue.Dequeue());
        }

        return result;
    }
}

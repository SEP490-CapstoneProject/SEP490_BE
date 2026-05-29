using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Community.Application.DTOs;
using Community.Application.Interfaces;
using RecruitmentPlatform.Contracts.DTOs;
using System.Security.Claims;
using System.Text.Json;

namespace Community.API.Controllers;

[ApiController]
[Route("api/community")]
public class CommunityController : ControllerBase
{
    private const int ActivePostStatus = 1;

    private readonly ICommunityService _service;
    private readonly ILogger<CommunityController> _logger;
    private readonly IHttpClientFactory _httpClientFactory;

    public CommunityController(
       ICommunityService service,
       ILogger<CommunityController> logger,
       IHttpClientFactory httpClientFactory)
    {
       _service = service;
       _logger = logger;
       _httpClientFactory = httpClientFactory;
    }

    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? User.FindFirst("sub")?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    private string? GetCurrentRole()
        => User.FindFirst(ClaimTypes.Role)?.Value;

    private bool IsAdminOrModerator()
    {
        var role = GetCurrentRole();
        return string.Equals(role, "ADMIN", StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, "MODERATOR", StringComparison.OrdinalIgnoreCase);
    }

    // ─── Feed endpoints (Require Authentication) ──────────────────────────────────────────────

    [Authorize]
    [HttpGet("posts")]
    public async Task<IActionResult> GetFeed(
        [FromQuery] int pageSize = 20,
        [FromQuery] int? cursor = null,
        [FromQuery] string? q = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            
            // 1. Fetch normal community posts
            var normalFeed = await _service.GetFeedAsync(cursor, pageSize, userId, q);

            // Map to UnifiedFeedItemDto
            var normalItems = normalFeed.Items
                .Select(p => new UnifiedFeedItemDto
                {
                    Type = "CommunityPost",
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
                hasMore = normalFeed.HasMore,
                pageSize = pageSize
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching community feed with sponsorship");
            return StatusCode(500, new { error = "Failed to fetch feed" });
        }
    }

    [Authorize]
    [HttpGet("posts/{id:int}")]
    public async Task<IActionResult> GetPostById(int id)
    {
        var userId = GetCurrentUserId();
        var postEntity = await _service.GetPostByIdAsync(id);
        if (postEntity == null || postEntity.Status != ActivePostStatus)
            return NotFound(new { error = $"Post {id} not found" });

        var post = await _service.GetPostDtoAsync(id, userId);
        if (post == null) return NotFound(new { error = $"Post {id} not found" });
        return Ok(post);
    }

    [Authorize]
    [HttpGet("posts/{postId:int}/comments")]
    public async Task<IActionResult> GetComments(int postId)
    {
        var response = await _service.GetCommentsResponseAsync(postId);
        return Ok(response);
    }

    [Authorize]
    [HttpGet("posts/user/{userId:int}")]
    public async Task<IActionResult> GetPostsByUser(int userId)
    {
        var currentUserId = GetCurrentUserId();
        var posts = await _service.GetPostsByUserIdDtoAsync(userId, currentUserId);
        return Ok(posts);
    }

    // ─── Post management ──────────────────────────────────────────────────────

    [Authorize]
    [HttpPost("posts")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreatePost(
        [FromForm] string postJson,
        [FromForm] List<IFormFile>? files)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new { error = "Invalid token" });

        CreatePostRequest request;
        try
        {
            request = JsonSerializer.Deserialize<CreatePostRequest>(postJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new CreatePostRequest();
        }
        catch
        {
            return BadRequest(new { error = "Invalid postJson format" });
        }

        var fileMap = files != null && files.Count > 0
            ? files.GroupBy(f => Path.GetFileName(f.FileName))
                   .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, IFormFile>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var created = await _service.CreatePostAsync(request, userId.Value, fileMap);
            
            if (created != null && created.ReviewStatus == 4)
            {
                // Rejected by auto-moderation
                return StatusCode(400, new { message = "Post was rejected by content moderation", reason = created.ReviewReason, data = created });
            }
            
            if (created != null && created.ReviewStatus == 3)
            {
                // Pending manual review
                return StatusCode(202, new { message = "Post awaiting manual review", reason = created.ReviewReason, data = created });
            }
            
            // Approved
            return StatusCode(201, created);
        }
        catch (BadHttpRequestException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [Authorize]
    [HttpPut("posts/{id:int}")]
    public async Task<IActionResult> UpdatePost(int id, [FromBody] UpdatePostRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new { error = "Invalid token" });

        var post = await _service.GetPostByIdAsync(id);
        if (post == null) return NotFound(new { error = $"Post {id} not found" });

        if (post.UserId != userId.Value && !IsAdminOrModerator())
            return StatusCode(403, new { error = "You don't have permission to update this post" });

        if (request.Description != null) post.Description = request.Description;
        if (request.PortfolioId.HasValue) post.PortfolioId = request.PortfolioId;
        if (request.Status.HasValue) post.Status = request.Status.Value;

        await _service.UpdatePostAsync(post);
        return NoContent();
    }

    [Authorize]
    [HttpDelete("posts/{id:int}")]
    public async Task<IActionResult> DeletePost(int id)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new { error = "Invalid token" });
        var role = GetCurrentRole();

        var post = await _service.GetPostByIdAsync(id);
        if (post == null) return NotFound(new { error = $"Post {id} not found" });

        if (post.UserId != userId.Value && !IsAdminOrModerator())
            return StatusCode(403, new { error = "You don't have permission to delete this post" });

        await _service.DeletePostAsync(id, userId.Value, role);
        return NoContent();
    }

    // ─── Save/Favorite ────────────────────────────────────────────────────────

    [Authorize]
    [HttpPost("posts/{postId:int}/save")]
    public async Task<IActionResult> SavePost(int postId)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new { error = "Invalid token" });

        var result = await _service.SavePostAsync(postId, userId.Value);
        if (!result) return BadRequest(new { error = "Post already saved" });
        return Ok(new { message = "Post saved" });
    }

    [Authorize]
    [HttpDelete("posts/{postId:int}/save")]
    public async Task<IActionResult> UnsavePost(int postId)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new { error = "Invalid token" });

        var result = await _service.UnsavePostAsync(postId, userId.Value);
        if (!result) return NotFound(new { error = "Save not found" });
        return NoContent();
    }

    [Authorize]
    [HttpPost("posts/{postId:int}/favorite")]
    public async Task<IActionResult> FavoritePost(int postId)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new { error = "Invalid token" });

        var result = await _service.FavoritePostAsync(postId, userId.Value);
        if (!result) return BadRequest(new { error = "Post already favorited" });
        return Ok(new { message = "Post favorited" });
    }

    [Authorize]
    [HttpDelete("posts/{postId:int}/favorite")]
    public async Task<IActionResult> UnfavoritePost(int postId)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new { error = "Invalid token" });

        var result = await _service.UnfavoritePostAsync(postId, userId.Value);
        if (!result) return NotFound(new { error = "Favorite not found" });
        return NoContent();
    }

    [Authorize]
    [HttpGet("posts/saved")]
    public async Task<IActionResult> GetSavedPosts()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new { error = "Invalid token" });

        var posts = await _service.GetSavedPostsAsync(userId.Value);
        return Ok(posts);
    }

    [Authorize]
    [HttpGet("posts/favorited")]
    public async Task<IActionResult> GetFavoritedPosts()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new { error = "Invalid token" });

        var posts = await _service.GetFavoritedPostsAsync(userId.Value);
        return Ok(posts);
    }

    // ─── Comment operations ───────────────────────────────────────────────────

    [Authorize]
    [HttpPost("posts/{postId:int}/comments")]
    public async Task<IActionResult> AddComment(int postId, [FromBody] AddCommentRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new { error = "Invalid token" });

        var comment = await _service.AddCommentAsync(postId, userId.Value, request.Content);
        return StatusCode(201, comment);
    }

    [Authorize]
    [HttpDelete("comments/{commentId:int}")]
    public async Task<IActionResult> DeleteComment(int commentId)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new { error = "Invalid token" });

        var ownerId = await _service.GetCommentOwnerAsync(commentId);
        if (ownerId == null) return NotFound(new { error = $"Comment {commentId} not found" });

        if (ownerId.Value != userId.Value && !IsAdminOrModerator())
            return StatusCode(403, new { error = "You don't have permission to delete this comment" });

        await _service.DeleteCommentAsync(commentId);
        return NoContent();
    }

    // ─── Reply operations ─────────────────────────────────────────────────────

    [Authorize]
    [HttpPost("comments/{commentId:int}/replies")]
    public async Task<IActionResult> AddReply(int commentId, [FromBody] AddReplyRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new { error = "Invalid token" });

        var reply = await _service.AddReplyAsync(commentId, userId.Value, request.ReplyToUserId, request.Content);
        return StatusCode(201, reply);
    }

    [Authorize]
    [HttpDelete("replies/{replyId:int}")]
    public async Task<IActionResult> DeleteReply(int replyId)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new { error = "Invalid token" });

        var ownerId = await _service.GetReplyOwnerAsync(replyId);
        if (ownerId == null) return NotFound(new { error = $"Reply {replyId} not found" });

        if (ownerId.Value != userId.Value && !IsAdminOrModerator())
            return StatusCode(403, new { error = "You don't have permission to delete this reply" });

        await _service.DeleteReplyAsync(replyId);
        return NoContent();
    }

    // ─── Post report & moderation ─────────────────────────────────────────────

    [Authorize]
    [HttpPost("posts/{postId:int}/report")]
    public async Task<IActionResult> ReportPost(int postId, [FromBody] CreatePostReportRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized(new { error = "Invalid token" });

        try
        {
            var created = await _service.ReportPostAsync(postId, userId.Value, request);
            return StatusCode(201, created);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
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

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;
using System.Security.Claims;

namespace Portfolio.API.Controllers;

[ApiController]
[Route("api/points")]
[Authorize(Roles = "RECRUITER,ADMIN,MODERATOR,EXPERT")]
public class RewardPointsController : ControllerBase
{
    private readonly IRewardPointsService _pointsService;
    private readonly ISponsoredPostService _sponsoredPostService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<RewardPointsController> _logger;

    public RewardPointsController(
        IRewardPointsService pointsService,
        ISponsoredPostService sponsoredPostService,
        ICurrentUserService currentUser,
        ILogger<RewardPointsController> logger)
    {
        _pointsService = pointsService;
        _sponsoredPostService = sponsoredPostService;
        _currentUser = currentUser;
        _logger = logger;
    }

    /// <summary>
    /// Get current point balance for the authenticated recruiter
    /// </summary>
    [HttpGet("balance")]
    public async Task<IActionResult> GetBalance()
    {
        try
        {
            if (_currentUser.UserId <= 0)
                return Unauthorized(new { error = "User not authenticated" });

            var balance = await _pointsService.GetPointBalanceAsync(_currentUser.UserId);
            return Ok(balance);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting point balance for user {UserId}", _currentUser.UserId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get transaction history for the authenticated recruiter
    /// </summary>
    [HttpGet("transactions")]
    public async Task<IActionResult> GetTransactions([FromQuery] int? days = null)
    {
        try
        {
            if (_currentUser.UserId <= 0)
                return Unauthorized(new { error = "User not authenticated" });

            if (days.HasValue && days.Value <= 0)
                return BadRequest(new { error = "Days must be greater than 0" });

            var transactions = await _pointsService.GetPointTransactionsAsync(_currentUser.UserId, days);
            return Ok(transactions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting transactions for user {UserId}", _currentUser.UserId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Create a sponsored post using reward points
    /// Supported content types: Text, Image, Video
    /// Points conversion: 5pts=1day, 12pts=3days, or custom amount
    /// </summary>
    [HttpPost("sponsored-posts")]
    public async Task<IActionResult> CreateSponsoredPost([FromBody] CreateSponsoredPostRequest request)
    {
        try
        {
            if (_currentUser.UserId <= 0)
                return Unauthorized(new { error = "User not authenticated" });

            if (request == null)
                return BadRequest(new { error = "Request body is required" });

            var result = await _sponsoredPostService.CreateSponsoredPostAsync(_currentUser.UserId, request);
            return StatusCode(201, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating sponsored post for user {UserId}", _currentUser.UserId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get my active sponsored posts
    /// </summary>
    [HttpGet("sponsored-posts/my")]
    public async Task<IActionResult> GetMyPosts()
    {
        try
        {
            if (_currentUser.UserId <= 0)
                return Unauthorized(new { error = "User not authenticated" });

            var posts = await _sponsoredPostService.GetRecruiterPostsAsync(_currentUser.UserId);
            return Ok(posts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recruiter's posts for user {UserId}", _currentUser.UserId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get active sponsored posts (paginated)
    /// </summary>
    [HttpGet("sponsored-posts")]
    [AllowAnonymous]
    public async Task<IActionResult> GetActivePosts([FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        try
        {
            if (skip < 0 || take <= 0 || take > 100)
                return BadRequest(new { error = "Skip must be >= 0, take must be 1-100" });

            var posts = await _sponsoredPostService.GetActivePostsAsync(skip, take);
            return Ok(posts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting active sponsored posts");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Get a specific sponsored post by ID
    /// </summary>
    [HttpGet("sponsored-posts/{postId:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPost(int postId)
    {
        try
        {
            var post = await _sponsoredPostService.GetPostAsync(postId);
            if (post == null)
                return NotFound(new { error = "Post not found" });

            return Ok(post);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting sponsored post {PostId}", postId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Pause a sponsored post (only creator or admin)
    /// </summary>
    [HttpPatch("sponsored-posts/{postId:int}/pause")]
    public async Task<IActionResult> PausePost(int postId)
    {
        try
        {
            if (_currentUser.UserId <= 0)
                return Unauthorized(new { error = "User not authenticated" });

            await _sponsoredPostService.PausePostAsync(postId, _currentUser.UserId, _currentUser.IsAdmin);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error pausing sponsored post {PostId} for user {UserId}", postId, _currentUser.UserId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Resume a paused sponsored post (only creator or admin)
    /// </summary>
    [HttpPatch("sponsored-posts/{postId:int}/resume")]
    public async Task<IActionResult> ResumePost(int postId)
    {
        try
        {
            if (_currentUser.UserId <= 0)
                return Unauthorized(new { error = "User not authenticated" });

            await _sponsoredPostService.ResumePostAsync(postId, _currentUser.UserId, _currentUser.IsAdmin);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resuming sponsored post {PostId} for user {UserId}", postId, _currentUser.UserId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Delete a sponsored post (only creator or admin)
    /// </summary>
    [HttpDelete("sponsored-posts/{postId:int}")]
    public async Task<IActionResult> DeletePost(int postId)
    {
        try
        {
            if (_currentUser.UserId <= 0)
                return Unauthorized(new { error = "User not authenticated" });

            await _sponsoredPostService.DeletePostAsync(postId, _currentUser.UserId, _currentUser.IsAdmin);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting sponsored post {PostId} for user {UserId}", postId, _currentUser.UserId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Record a view for analytics (called when post is displayed)
    /// </summary>
    [HttpPost("sponsored-posts/{postId:int}/view")]
    [AllowAnonymous]
    public async Task<IActionResult> RecordView(int postId)
    {
        try
        {
            await _sponsoredPostService.RecordViewAsync(postId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error recording view for post {PostId}", postId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Record a click for analytics (called when post link is clicked)
    /// </summary>
    [HttpPost("sponsored-posts/{postId:int}/click")]
    [AllowAnonymous]
    public async Task<IActionResult> RecordClick(int postId)
    {
        try
        {
            await _sponsoredPostService.RecordClickAsync(postId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error recording click for post {PostId}", postId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

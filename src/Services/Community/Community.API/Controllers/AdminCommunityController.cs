using Community.Application.DTOs;
using Community.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Community.API.Controllers;

[ApiController]
[Route("api/community/admin")]
[Authorize]
public class AdminCommunityController : ControllerBase
{
    private readonly ICommunityService _service;

    public AdminCommunityController(ICommunityService service)
    {
        _service = service;
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

    [HttpGet("posts")]
    public async Task<IActionResult> GetPosts([FromQuery] AdminPostFilter filter)
    {
        if (!IsAdminOrModerator())
            return StatusCode(403, new { error = "Only admin/moderator can access admin posts." });

        var currentUserId = GetCurrentUserId();

        try
        {
            var posts = await _service.GetAdminPostsAsync(filter, currentUserId);
            return Ok(posts);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("posts/pending")]
    public async Task<IActionResult> GetPendingPosts([FromQuery] int skip = 0, [FromQuery] int limit = 20)
    {
        if (!IsAdminOrModerator())
            return StatusCode(403, new { error = "Only admin/moderator can access pending posts." });

        try
        {
            var result = await _service.GetPendingPostsAsync(skip, limit);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("posts/{postId:int}/approve")]
    public async Task<IActionResult> ApprovePost(int postId, [FromBody] ApprovePostRequest request)
    {
        if (!IsAdminOrModerator())
            return StatusCode(403, new { error = "Only admin/moderator can approve posts." });

        try
        {
            var post = await _service.ApprovePostAsync(postId, request?.ApproverNotes);
            return Ok(post);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("posts/{postId:int}/reject")]
    public async Task<IActionResult> RejectPost(int postId, [FromBody] RejectPostRequest request)
    {
        if (!IsAdminOrModerator())
            return StatusCode(403, new { error = "Only admin/moderator can reject posts." });

        if (string.IsNullOrWhiteSpace(request?.Reason))
            return BadRequest(new { error = "Reason is required" });

        try
        {
            var post = await _service.RejectPostAsync(postId, request.Reason);
            return Ok(post);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("reports")]
    public async Task<IActionResult> GetPostReports([FromQuery] AdminPostReportFilter filter)
    {
        if (!IsAdminOrModerator())
            return StatusCode(403, new { error = "Only admin/moderator can review reports." });

        try
        {
            var reports = await _service.GetPostReportsAsync(filter);
            return Ok(reports);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("reports/{reportId:int}/review")]
    public async Task<IActionResult> ReviewPostReport(int reportId, [FromBody] ReviewPostReportRequest request)
    {
        if (!IsAdminOrModerator())
            return StatusCode(403, new { error = "Only admin/moderator can review reports." });

        var reviewerUserId = GetCurrentUserId();
        if (reviewerUserId == null) return Unauthorized(new { error = "Invalid token" });

        try
        {
            var reviewed = await _service.ReviewPostReportAsync(reportId, reviewerUserId.Value, request);
            return Ok(reviewed);
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
}

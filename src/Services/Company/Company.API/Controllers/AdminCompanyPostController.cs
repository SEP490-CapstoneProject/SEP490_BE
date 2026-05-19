using Company.Application.DTOs;
using Company.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Company.API.Controllers;

[ApiController]
[Route("api/admin/company-posts")]
[Authorize]
public class AdminCompanyPostController : ControllerBase
{
    private readonly ICompanyPostService _service;

    public AdminCompanyPostController(ICompanyPostService service)
    {
        _service = service;
    }

    private string? GetCurrentRole()
        => User.FindFirst(ClaimTypes.Role)?.Value;

    private bool IsAdminOrModerator()
    {
        var role = GetCurrentRole();
        return string.Equals(role, "ADMIN", StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, "MODERATOR", StringComparison.OrdinalIgnoreCase);
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPendingPosts([FromQuery] int skip = 0, [FromQuery] int limit = 20)
    {
        if (!IsAdminOrModerator())
            return StatusCode(403, new { error = "Only admin/moderator can access pending posts." });

        try
        {
            var posts = await _service.GetPendingPostsAsync(skip, limit);
            return Ok(new { data = posts, total = posts.Count });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{postId:int}/approve")]
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

    [HttpPost("{postId:int}/reject")]
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
    public async Task<IActionResult> GetPostReports([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        if (!IsAdminOrModerator())
            return StatusCode(403, new { error = "Only admin/moderator can review reports." });

        try
        {
            var reports = await _service.GetPostReportsAsync(page, pageSize);
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

        var reviewerUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (!int.TryParse(reviewerUserIdClaim, out var reviewerUserId))
            return Unauthorized(new { error = "Invalid token" });

        try
        {
            var reviewed = await _service.ReviewPostReportAsync(reportId, reviewerUserId, request);
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

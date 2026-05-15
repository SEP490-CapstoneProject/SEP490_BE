using Company.Application.DTOs;
using Company.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Security.Claims;

namespace Company.API.Controllers;

[ApiController]
[Route("api/company-posts")]
public class CompanyPostController : ControllerBase
{
    private readonly ICompanyPostService _service;

    public CompanyPostController(ICompanyPostService service)
    {
        _service = service;
    }

    private int? GetUserId()
    {
        var userIdRaw = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst("nameid")?.Value
            ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
        return int.TryParse(userIdRaw, out var id) ? id : null;
    }

    private int? GetCompanyId()
    {
        var companyIdRaw = User.FindFirst("companyId")?.Value;
        return int.TryParse(companyIdRaw, out var id) ? id : null;
    }

    private int GetRequiredUserId()
    {
        var id = GetUserId();
        if (id == null) throw new UnauthorizedAccessException();
        return id.Value;
    }

    private int GetRequiredCompanyId()
    {
        var id = GetCompanyId();
        if (id == null) throw new UnauthorizedAccessException();
        return id.Value;
    }

    /// <summary>Get paginated job post feed (all companies)</summary>
    [HttpGet]
    public async Task<IActionResult> GetFeed(
        [FromQuery] DateTime? cursor,
        [FromQuery] int limit = 10)
    {
        var userId = GetUserId();
        var result = await _service.GetPostFeedAsync(cursor, limit, userId);
        return Ok(result);
    }

    /// <summary>Get all posts from a specific company</summary>
    [HttpGet("company/{companyId:int}")]
    public async Task<IActionResult> GetByCompany(
        int companyId,
        [FromQuery] DateTime? cursor,
        [FromQuery] int limit = 10)
    {
        var userId = GetUserId();
        var result = await _service.GetPostsByCompanyAsync(companyId, cursor, limit, userId);
        return Ok(result);
    }

    /// <summary>Get saved job posts of current user</summary>
    [HttpGet("saved")]
    [Authorize]
    public async Task<IActionResult> GetSavedPosts(
        [FromQuery] DateTime? cursor,
        [FromQuery] int limit = 10)
    {
        int userId;
        try { userId = GetRequiredUserId(); }
        catch { return Unauthorized(new { message = "Authentication required" }); }

        var result = await _service.GetSavedPostsAsync(cursor, limit, userId);
        return Ok(result);
    }

    /// <summary>Get job post detail with all media</summary>
    [HttpGet("batch")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByIds([FromQuery] string ids)
    {
        if (string.IsNullOrWhiteSpace(ids))
        {
            return BadRequest(new { message = "ids is required" });
        }

        var postIds = ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(id => int.TryParse(id, out var parsed) ? parsed : (int?)null)
            .Where(id => id.HasValue && id.Value > 0)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        if (postIds.Count == 0)
        {
            return Ok(new List<CompanyPostDetailDto>());
        }

        var result = await _service.GetPostsByIdsAsync(postIds, GetUserId());
        return Ok(result);
    }

    /// <summary>Get job post detail with all media</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetDetail(int id)
    {
        var userId = GetUserId();
        var result = await _service.GetPostDetailAsync(id, userId);
        if (result == null) return NotFound(new { message = "Post not found" });
        return Ok(result);
    }

    [HttpGet("{id:int}/match-portfolios")]
    [Authorize]
    public async Task<IActionResult> MatchPortfolios(
        int id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.MatchPortfoliosForJobAsync(id, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("internal/matching-candidates")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMatchingCandidates(
        [FromQuery] int limit = 150,
        CancellationToken cancellationToken = default)
    {
        var result = await _service.GetMatchingCandidatesAsync(limit, cancellationToken);
        return Ok(result);
    }

    /// <summary>Create a job post with optional media files</summary>
    [HttpPost]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreatePost(
        [FromForm] string postJson,
        [FromForm] List<IFormFile>? files)
    {
        int companyId;
        try { companyId = GetRequiredCompanyId(); }
        catch { return Unauthorized(new { message = "Authentication required" }); }

        if (string.IsNullOrWhiteSpace(postJson))
            return BadRequest(new { message = "postJson is required" });

        CreatePostRequest? request;
        try
        {
            request = System.Text.Json.JsonSerializer.Deserialize<CreatePostRequest>(postJson,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Invalid postJson", detail = ex.Message });
        }

        if (request == null) return BadRequest(new { message = "Invalid postJson" });

        var fileMap = files != null && files.Count > 0
            ? files.GroupBy(f => Path.GetFileName(f.FileName))
                   .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, IFormFile>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var result = await _service.CreatePostAsync(request, companyId, fileMap);
            
            if (result != null && result.ReviewStatus == 4)
            {
                // Rejected by auto-moderation
                return StatusCode(400, new { message = "Post was rejected by content moderation", reason = result.ReviewReason, data = result });
            }
            
            if (result != null && result.ReviewStatus == 3)
            {
                // Pending manual review
                return StatusCode(202, new { message = "Post awaiting manual review", reason = result.ReviewReason, data = result });
            }
            
            // Approved
            return CreatedAtAction(nameof(GetDetail), new { id = result.PostId }, result);
        }
        catch (BadHttpRequestException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Update job post info (owner only)</summary>
    [HttpPut("{id:int}")]
    [Authorize]
    public async Task<IActionResult> UpdatePost(int id, [FromBody] UpdatePostRequest request)
    {
        int userId;
        try { userId = GetRequiredCompanyId(); }
        catch { return Unauthorized(new { message = "Authentication required" }); }

        var result = await _service.UpdatePostAsync(id, request, userId);
        if (result == null) return NotFound(new { message = "Post not found or unauthorized" });
        return Ok(result);
    }

    /// <summary>Full update job post with optional media files (owner only)</summary>
    [HttpPut("{id:int}/full")]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdatePostFull(
        int id,
        [FromForm] string postJson,
        [FromForm] List<IFormFile>? files)
    {
        int companyId;
        try { companyId = GetRequiredCompanyId(); }
        catch { return Unauthorized(new { message = "Authentication required" }); }

        if (string.IsNullOrWhiteSpace(postJson))
            return BadRequest(new { message = "postJson is required" });

        UpdatePostFullRequest? request;
        try
        {
            request = System.Text.Json.JsonSerializer.Deserialize<UpdatePostFullRequest>(postJson,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Invalid postJson", detail = ex.Message });
        }

        if (request == null) return BadRequest(new { message = "Invalid postJson" });

        var fileMap = files != null && files.Count > 0
            ? files.GroupBy(f => Path.GetFileName(f.FileName))
                   .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, IFormFile>(StringComparer.OrdinalIgnoreCase);

        try
        {
            var result = await _service.UpdatePostFullAsync(id, request, companyId, fileMap);
            if (result == null) return NotFound(new { message = "Post not found or unauthorized" });
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>Soft-delete a job post (owner only)</summary>
    [HttpDelete("{id:int}")]
    [Authorize]
    public async Task<IActionResult> DeletePost(int id)
    {
        int userId;
        try { userId = GetRequiredCompanyId(); }
        catch { return Unauthorized(new { message = "Authentication required" }); }

        var success = await _service.SoftDeletePostAsync(id, userId);
        if (!success) return NotFound(new { message = "Post not found or unauthorized" });
        return NoContent();
    }

    /// <summary>Save a job post (logged-in users)</summary>
    [HttpPost("{id:int}/save")]
    [Authorize]
    public async Task<IActionResult> SavePost(int id)
    {
        int userId;
        try { userId = GetRequiredUserId(); }
        catch { return Unauthorized(new { message = "Authentication required" }); }

        await _service.SavePostAsync(id, userId);
        return Ok(new { message = "Post saved" });
    }

    /// <summary>Unsave a job post (logged-in users)</summary>
    [HttpDelete("{id:int}/save")]
    [Authorize]
    public async Task<IActionResult> UnsavePost(int id)
    {
        int userId;
        try { userId = GetRequiredUserId(); }
        catch { return Unauthorized(new { message = "Authentication required" }); }

        await _service.UnsavePostAsync(id, userId);
        return Ok(new { message = "Post unsaved" });
    }

    /// <summary>Report a job post</summary>
    [Authorize]
    [HttpPost("{postId:int}/report")]
    public async Task<IActionResult> ReportPost(int postId, [FromBody] CreatePostReportRequest request)
    {
        var userId = GetUserId();
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
}

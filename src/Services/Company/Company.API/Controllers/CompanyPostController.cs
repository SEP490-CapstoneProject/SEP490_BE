using Company.Application.DTOs;
using Company.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;

namespace Company.API.Controllers;

[ApiController]
[Route("api/company")]
public class CompanyPostController : ControllerBase
{
    private readonly ICompanyPostService _service;

    public CompanyPostController(ICompanyPostService service)
    {
        _service = service;
    }

    private int? GetUserId()
    {
        var auth = Request.Headers.Authorization.FirstOrDefault();
        if (string.IsNullOrEmpty(auth) || !auth.StartsWith("Bearer ")) return null;

        var token = auth["Bearer ".Length..].Trim();
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            var sub = jwt.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type == "nameid")?.Value;
            return int.TryParse(sub, out var id) ? id : null;
        }
        catch { return null; }
    }

    private int GetRequiredUserId()
    {
        var id = GetUserId();
        if (id == null) throw new UnauthorizedAccessException();
        return id.Value;
    }

    /// <summary>Get paginated job post feed (all companies)</summary>
    [HttpGet("posts")]
    public async Task<IActionResult> GetFeed(
        [FromQuery] DateTime? cursor,
        [FromQuery] int limit = 10)
    {
        var userId = GetUserId();
        var result = await _service.GetPostFeedAsync(cursor, limit, userId);
        return Ok(result);
    }

    /// <summary>Get all posts from a specific company</summary>
    [HttpGet("{companyId:int}/posts")]
    public async Task<IActionResult> GetByCompany(
        int companyId,
        [FromQuery] DateTime? cursor,
        [FromQuery] int limit = 10)
    {
        var userId = GetUserId();
        var result = await _service.GetPostsByCompanyAsync(companyId, cursor, limit, userId);
        return Ok(result);
    }

    /// <summary>Get job post detail with all media</summary>
    [HttpGet("posts/{id:int}")]
    public async Task<IActionResult> GetDetail(int id)
    {
        var userId = GetUserId();
        var result = await _service.GetPostDetailAsync(id, userId);
        if (result == null) return NotFound(new { message = "Post not found" });
        return Ok(result);
    }

    /// <summary>Create a job post with optional media files</summary>
    [HttpPost("posts")]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CreatePost([FromForm] IFormCollection form)
    {
        int companyId;
        try { companyId = GetRequiredUserId(); }
        catch { return Unauthorized(new { message = "Authentication required" }); }

        var postJson = form["postJson"].FirstOrDefault();
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

        var fileMap = form.Files
            .GroupBy(f => f.FileName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var result = await _service.CreatePostAsync(request, companyId, fileMap);
        return CreatedAtAction(nameof(GetDetail), new { id = result.PostId }, result);
    }

    /// <summary>Update job post info (owner only)</summary>
    [HttpPut("posts/{id:int}")]
    [Authorize]
    public async Task<IActionResult> UpdatePost(int id, [FromBody] UpdatePostRequest request)
    {
        int userId;
        try { userId = GetRequiredUserId(); }
        catch { return Unauthorized(new { message = "Authentication required" }); }

        var result = await _service.UpdatePostAsync(id, request, userId);
        if (result == null) return NotFound(new { message = "Post not found or unauthorized" });
        return Ok(result);
    }

    /// <summary>Soft-delete a job post (owner only)</summary>
    [HttpDelete("posts/{id:int}")]
    [Authorize]
    public async Task<IActionResult> DeletePost(int id)
    {
        int userId;
        try { userId = GetRequiredUserId(); }
        catch { return Unauthorized(new { message = "Authentication required" }); }

        var success = await _service.SoftDeletePostAsync(id, userId);
        if (!success) return NotFound(new { message = "Post not found or unauthorized" });
        return NoContent();
    }

    /// <summary>Save a job post (logged-in users)</summary>
    [HttpPost("posts/{id:int}/save")]
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
    [HttpDelete("posts/{id:int}/save")]
    [Authorize]
    public async Task<IActionResult> UnsavePost(int id)
    {
        int userId;
        try { userId = GetRequiredUserId(); }
        catch { return Unauthorized(new { message = "Authentication required" }); }

        await _service.UnsavePostAsync(id, userId);
        return Ok(new { message = "Post unsaved" });
    }
}

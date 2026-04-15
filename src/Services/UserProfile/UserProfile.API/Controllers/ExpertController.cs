using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UserProfile.Application.DTOs;
using UserProfile.Application.Interfaces;

namespace UserProfile.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ExpertController : ControllerBase
{
    private readonly IExpertService _service;

    public ExpertController(IExpertService service)
    {
        _service = service;
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMyProfile()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized(new { error = "Invalid or missing user ID in token" });
        }

        var expert = await _service.GetByUserIdAsync(userId);
        if (expert == null)
        {
            return NotFound(new { error = "Expert profile not found" });
        }

        return Ok(expert);
    }

    [HttpGet("batch")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBatch([FromQuery] string userIds)
    {
        if (string.IsNullOrWhiteSpace(userIds)) return Ok(new List<object>());

        var ids = userIds.Split(',')
            .Select(s => int.TryParse(s.Trim(), out var id) ? (int?)id : null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        if (ids.Count == 0) return Ok(new List<object>());

        var all = await _service.GetAllAsync();
        var result = all.Where(e => ids.Contains(e.UserId)).ToList();
        return Ok(result);
    }

    [HttpGet("by-user/{userId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByUserId(int userId)
    {
        var expert = await _service.GetByUserIdAsync(userId);
        if (expert == null)
        {
            return NotFound(new { error = "Expert profile not found" });
        }

        return Ok(expert);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var expert = await _service.GetByIdAsync(id);
        if (expert == null)
        {
            return NotFound(new { error = $"Expert with ID {id} not found" });
        }

        return Ok(expert);
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var experts = await _service.GetAllAsync();
        return Ok(experts);
    }

    [HttpPost]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create([FromForm] CreateExpertRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized(new { error = "Invalid or missing user ID in token" });
        }

        try
        {
            var expert = await _service.CreateAsync(userId, request, request.Avatar, request.CoverImage);
            return CreatedAtAction(nameof(GetById), new { id = expert.Id }, expert);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Update(int id, [FromForm] UpdateExpertRequest request)
    {
        try
        {
            var expert = await _service.UpdateAsync(id, request, request.Avatar, request.CoverImage);
            return Ok(expert);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteAsync(id);
        if (!result)
        {
            return NotFound(new { error = $"Expert with ID {id} not found" });
        }

        return NoContent();
    }
}

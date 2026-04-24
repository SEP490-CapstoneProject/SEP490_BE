using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;

namespace Portfolio.API.Controllers;

/// <summary>
/// Public endpoint: GET /api/criteria — only active criteria
/// Admin endpoints: /api/admin/criteria — full CRUD + toggle
/// </summary>
[ApiController]
public class CriterionController : ControllerBase
{
    private readonly ICriterionService _service;
    private readonly ILogger<CriterionController> _logger;

    public CriterionController(ICriterionService service, ILogger<CriterionController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // ── Public ────────────────────────────────────────────────────────────────

    /// <summary>Lấy danh sách tiêu chí đang hoạt động (public).</summary>
    [HttpGet("api/criteria")]
    [AllowAnonymous]
    public async Task<IActionResult> GetActive()
    {
        var list = await _service.GetActiveAsync();
        return Ok(list);
    }

    /// <summary>Lấy chi tiết một tiêu chí theo ID (public).</summary>
    [HttpGet("api/criteria/{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var result = await _service.GetByIdAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting criterion {Id}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    // ── Admin ─────────────────────────────────────────────────────────────────

    /// <summary>Lấy tất cả tiêu chí (admin).</summary>
    [HttpGet("api/admin/criteria")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> GetAll()
    {
        var list = await _service.GetAllAsync();
        return Ok(list);
    }

    /// <summary>Tạo tiêu chí mới (admin).</summary>
    [HttpPost("api/admin/criteria")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Create([FromBody] CreateCriterionRequest request)
    {
        try
        {
            var result = await _service.CreateAsync(request);
            return StatusCode(201, result);
        }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating criterion");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>Cập nhật tiêu chí (admin).</summary>
    [HttpPut("api/admin/criteria/{id:int}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCriterionRequest request)
    {
        try
        {
            var result = await _service.UpdateAsync(id, request);
            return Ok(result);
        }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating criterion {Id}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>Bật/tắt trạng thái tiêu chí (admin).</summary>
    [HttpPatch("api/admin/criteria/{id:int}/toggle")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Toggle(int id)
    {
        try
        {
            var result = await _service.ToggleActiveAsync(id);
            var msg = result.IsActive ? "Criterion activated" : "Criterion deactivated";
            return Ok(new { message = msg, criterion = result });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling criterion {Id}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>Xoá mềm tiêu chí (admin).</summary>
    [HttpDelete("api/admin/criteria/{id:int}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Delete(int id)
    {
        try
        {
            await _service.DeleteAsync(id);
            return Ok(new { message = $"Criterion {id} deleted" });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting criterion {Id}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;

namespace Portfolio.API.Controllers;

/// <summary>
/// Public endpoint: GET /api/block-types — only active block types
/// Admin endpoints: /api/admin/block-types — full CRUD + toggle
/// </summary>
[ApiController]
public class BlockTypeController : ControllerBase
{
    private readonly IBlockTypeService _service;
    private readonly ILogger<BlockTypeController> _logger;

    public BlockTypeController(IBlockTypeService service, ILogger<BlockTypeController> logger)
    {
        _service = service;
        _logger = logger;
    }

    // ── Public ────────────────────────────────────────────────────────────────

    [HttpGet("api/block-types")]
    [AllowAnonymous]
    public async Task<IActionResult> GetActive()
    {
        var list = await _service.GetActiveAsync();
        return Ok(list);
    }

    // ── Admin ─────────────────────────────────────────────────────────────────

    [HttpGet("api/admin/block-types")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> GetAll()
    {
        var list = await _service.GetAllAsync();
        return Ok(list);
    }

    [HttpPost("api/admin/block-types")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Create([FromBody] CreateBlockTypeRequest request)
    {
        try
        {
            var result = await _service.CreateAsync(request);
            return StatusCode(201, result);
        }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating block type");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPut("api/admin/block-types/{id:int}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateBlockTypeRequest request)
    {
        try
        {
            var result = await _service.UpdateAsync(id, request);
            return Ok(result);
        }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating block type {Id}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPatch("api/admin/block-types/{id:int}/toggle")]
    [Authorize(Roles = "ADMIN")]
    public async Task<IActionResult> Toggle(int id)
    {
        try
        {
            var result = await _service.ToggleActiveAsync(id);
            var msg = result.IsActive ? "Block type activated" : "Block type deactivated";
            return Ok(new { message = msg, blockType = result });
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling block type {Id}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

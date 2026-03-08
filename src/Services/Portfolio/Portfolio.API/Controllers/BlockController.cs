using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;

namespace Portfolio.API.Controllers;

[ApiController]
[Route("api/portfolio/{portfolioId:int}/blocks")]
[Authorize]
public class BlockController : ControllerBase
{
    private readonly IBlockService _blockService;
    private readonly ILogger<BlockController> _logger;

    public BlockController(IBlockService blockService, ILogger<BlockController> logger)
    {
        _blockService = blockService;
        _logger = logger;
    }

    /// <summary>
    /// Add a block. Send blockJson (serialized AddBlockRequest) + optional files.
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> AddBlock(
        int portfolioId,
        [FromForm] string blockJson,
        [FromForm] List<IFormFile>? files)
    {
        var employeeId = GetEmployeeId();
        if (employeeId == null) return Unauthorized(new { error = "EmployeeId claim not found" });

        AddBlockRequest request;
        try
        {
            request = JsonSerializer.Deserialize<AddBlockRequest>(blockJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new ArgumentException("blockJson cannot be null");
        }
        catch (JsonException ex)
        {
            return BadRequest(new { error = $"Invalid JSON: {ex.Message}" });
        }

        var fileMap = Request.Form.Files
            .GroupBy(f => Path.GetFileName(f.FileName))
            .ToDictionary(g => g.Key, g => g.First());

        try
        {
            var block = await _blockService.AddBlockAsync(portfolioId, employeeId.Value, request, fileMap);
            return CreatedAtRoute(null, block);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding block to portfolio {PortfolioId}", portfolioId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    /// <summary>
    /// Update a block. Send blockJson (serialized UpdateBlockRequest) + optional files.
    /// </summary>
    [HttpPut("{blockId:int}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateBlock(
        int portfolioId,
        int blockId,
        [FromForm] string blockJson,
        [FromForm] List<IFormFile>? files)
    {
        var employeeId = GetEmployeeId();
        if (employeeId == null) return Unauthorized(new { error = "EmployeeId claim not found" });

        UpdateBlockRequest request;
        try
        {
            request = JsonSerializer.Deserialize<UpdateBlockRequest>(blockJson,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new ArgumentException("blockJson cannot be null");
        }
        catch (JsonException ex)
        {
            return BadRequest(new { error = $"Invalid JSON: {ex.Message}" });
        }

        var fileMap = Request.Form.Files
            .GroupBy(f => Path.GetFileName(f.FileName))
            .ToDictionary(g => g.Key, g => g.First());

        try
        {
            var block = await _blockService.UpdateBlockAsync(blockId, portfolioId, employeeId.Value, request, fileMap);
            return Ok(block);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating block {BlockId}", blockId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpDelete("{blockId:int}")]
    public async Task<IActionResult> DeleteBlock(int portfolioId, int blockId)
    {
        var employeeId = GetEmployeeId();
        if (employeeId == null) return Unauthorized(new { error = "EmployeeId claim not found" });

        try
        {
            await _blockService.DeleteBlockAsync(blockId, portfolioId, employeeId.Value);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting block {BlockId}", blockId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPut("reorder")]
    public async Task<IActionResult> Reorder(int portfolioId, [FromBody] ReorderBlocksRequest request)
    {
        var employeeId = GetEmployeeId();
        if (employeeId == null) return Unauthorized(new { error = "EmployeeId claim not found" });

        try
        {
            await _blockService.ReorderBlocksAsync(portfolioId, employeeId.Value, request);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering blocks in portfolio {PortfolioId}", portfolioId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    private int? GetEmployeeId()
    {
        var claim = User.FindFirst("employeeId")?.Value ?? User.FindFirst("EmployeeId")?.Value;
        if (!string.IsNullOrEmpty(claim) && int.TryParse(claim, out var id)) return id;
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (!string.IsNullOrEmpty(sub) && int.TryParse(sub, out var sid)) return sid;
        return null;
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;
using Portfolio.Application.Services;

namespace Portfolio.API.Controllers;

[ApiController]
[Route("api/portfolio")]
[Authorize]
public class PortfolioController : ControllerBase
{
    private readonly IPortfolioService _portfolioService;
    private readonly IBlockRepository _blockRepo;
    private readonly BlockService _blockService;
    private readonly ILogger<PortfolioController> _logger;

    public PortfolioController(
        IPortfolioService portfolioService,
        IBlockRepository blockRepo,
        BlockService blockService,
        ILogger<PortfolioController> logger)
    {
        _portfolioService = portfolioService;
        _blockRepo = blockRepo;
        _blockService = blockService;
        _logger = logger;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create([FromForm] string portfolioJson, [FromForm] List<IFormFile>? files)
    {
        CreatePortfolioRequest request;
        try
        {
            request = System.Text.Json.JsonSerializer.Deserialize<CreatePortfolioRequest>(
                portfolioJson,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new ArgumentException("portfolioJson cannot be null");
        }
        catch (System.Text.Json.JsonException ex)
        {
            return BadRequest(new { error = $"Invalid JSON: {ex.Message}" });
        }

        var fileMap = (files ?? new List<IFormFile>())
            .ToDictionary(f => Path.GetFileName(f.FileName), f => f);

        try
        {
            var result = await _portfolioService.CreatePortfolioAsync(request, fileMap);
            return StatusCode(201, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating portfolio");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        var portfolio = await _portfolioService.GetByIdAsync(id);
        if (portfolio == null) return NotFound(new { error = $"Portfolio {id} not found" });

        var blocks = await _blockRepo.GetByPortfolioIdAsync(id);
        portfolio.Blocks = blocks.Select(b => _blockService.MapBlockToDto(b)).ToList();

        return Ok(portfolio);
    }

    [HttpGet("employee/{employeeId:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByEmployee(int employeeId)
    {
        var portfolios = (await _portfolioService.GetByEmployeeIdAsync(employeeId)).ToList();

        foreach (var p in portfolios)
        {
            var blocks = await _blockRepo.GetByPortfolioIdAsync(p.PortfolioId);
            p.Blocks = blocks.Select(b => _blockService.MapBlockToDto(b)).ToList();
        }

        return Ok(portfolios);
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMine()
    {
        var employeeId = GetEmployeeId();
        if (employeeId == null) return Unauthorized(new { error = "EmployeeId claim not found" });

        var portfolios = (await _portfolioService.GetByEmployeeIdAsync(employeeId.Value)).ToList();
        foreach (var p in portfolios)
        {
            var blocks = await _blockRepo.GetByPortfolioIdAsync(p.PortfolioId);
            p.Blocks = blocks.Select(b => _blockService.MapBlockToDto(b)).ToList();
        }
        return Ok(portfolios);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePortfolioRequest request)
    {
        var employeeId = GetEmployeeId();
        if (employeeId == null) return Unauthorized(new { error = "EmployeeId claim not found" });

        try
        {
            var portfolio = await _portfolioService.UpdateAsync(id, employeeId.Value, request);
            return Ok(portfolio);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating portfolio {Id}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var employeeId = GetEmployeeId();
        if (employeeId == null) return Unauthorized(new { error = "EmployeeId claim not found" });

        try
        {
            await _portfolioService.DeleteAsync(id, employeeId.Value);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting portfolio {Id}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    private int? GetEmployeeId()
    {
        var claim = User.FindFirst("employeeId")?.Value
                 ?? User.FindFirst("EmployeeId")?.Value;
        if (!string.IsNullOrEmpty(claim) && int.TryParse(claim, out int empId))
            return empId;

        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
               ?? User.FindFirst("sub")?.Value;
        if (!string.IsNullOrEmpty(sub) && int.TryParse(sub, out int subId))
            return subId;

        return null;
    }
}


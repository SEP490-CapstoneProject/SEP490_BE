using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;
using Portfolio.Application.Services;
using Portfolio.Domain.Entities;

namespace Portfolio.API.Controllers;

[ApiController]
[Route("api/portfolio")]
[Authorize]
public class PortfolioController : ControllerBase
{
    private const string ActiveStatus = "active";
    private readonly IPortfolioService _portfolioService;
    private readonly IPortfolioPreviewService _portfolioPreviewService;
    private readonly IBlockRepository _blockRepo;
    private readonly BlockService _blockService;
    private readonly ILogger<PortfolioController> _logger;

    public PortfolioController(
        IPortfolioService portfolioService,
        IPortfolioPreviewService portfolioPreviewService,
        IBlockRepository blockRepo,
        BlockService blockService,
        ILogger<PortfolioController> logger)
    {
        _portfolioService = portfolioService;
        _portfolioPreviewService = portfolioPreviewService;
        _blockRepo = blockRepo;
        _blockService = blockService;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? status = null,
        [FromQuery] string? q = null,
        [FromQuery] string? blockType = null,
        [FromQuery] bool includeCompliments = false,
        [FromQuery] ComplimentState? complimentState = null,
        [FromQuery] bool? hasCompliment = null,
        [FromQuery] PortfolioRankBy rankBy = PortfolioRankBy.average,
        [FromQuery] PortfolioSortMode sort = PortfolioSortMode.newest)
    {
        try
        {
            // Use compliment filter path when any compliment param is specified
            if (includeCompliments || complimentState.HasValue || hasCompliment.HasValue)
            {
                var queryParams = new PortfolioQueryParams
                {
                    Page = page,
                    PageSize = pageSize,
                    Status = status,
                    SearchTerm = q,
                    BlockType = blockType,
                    IncludeCompliments = includeCompliments,
                    ComplimentState = complimentState,
                    HasCompliment = hasCompliment,
                    RankBy = rankBy,
                    Sort = sort
                };
                var filteredResult = await _portfolioService.GetAllWithComplimentFilterAsync(queryParams);

                foreach (var p in filteredResult.Items)
                {
                    var blocks = await _blockRepo.GetByPortfolioIdAsync(p.Id);
                    p.Blocks = blocks.Select(b => _blockService.MapBlockToDto(b)).ToList();
                }

                return Ok(filteredResult);
            }

            var result = await _portfolioService.GetAllAsync(page, pageSize, status, q, blockType, sort, rankBy);

            foreach (var p in result.Items)
            {
                var blocks = await _blockRepo.GetByPortfolioIdAsync(p.PortfolioId);
                p.Blocks = blocks.Select(b => _blockService.MapBlockToDto(b)).ToList();
            }

            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
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

        var fileMap = Request.Form.Files
            .GroupBy(f => Path.GetFileName(f.FileName))
            .ToDictionary(g => g.Key, g => g.First());

        try
        {
            var result = await _portfolioService.CreatePortfolioAsync(request, fileMap);
            
            // Return appropriate HTTP status based on moderation result
            if (result.ModerationStatus?.Equals("Rejected", StringComparison.OrdinalIgnoreCase) == true)
                return StatusCode(400, result);
            else if (result.ModerationStatus?.Equals("PendingReview", StringComparison.OrdinalIgnoreCase) == true)
                return StatusCode(202, result);
            else
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

    [HttpGet("{id:int}/preview")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPreview(int id)
    {
        var portfolio = await _portfolioService.GetByIdAsync(id);
        if (portfolio == null) return NotFound(new { error = $"Portfolio {id} not found" });
        if (!string.Equals(portfolio.Status, ActiveStatus, StringComparison.OrdinalIgnoreCase))
            return NotFound(new { error = $"Portfolio {id} not found" });

        var blocks = await _blockRepo.GetByPortfolioIdAsync(id);
        var firstBlock = blocks.OrderBy(b => b.DisplayOrder).FirstOrDefault();
        if (firstBlock == null) return NotFound(new { error = "No blocks found" });

        var dto = _blockService.MapBlockToDto(firstBlock);
        return Ok(new { type = dto.Type, variant = dto.Variant, data = dto.Data });
    }

    [HttpPost("{id:int}/preview/generate")]
    [Authorize]
    public async Task<IActionResult> GeneratePreview(int id, [FromBody] PortfolioPreviewGenerateRequest? request)
    {
        var result = await _portfolioPreviewService.GeneratePreviewAsync(id, request?.HighlightsDescription);
        if (result == null || !result.Success)
        {
            return BadRequest(new
            {
                success = false,
                message = result?.Message ?? "Failed to generate preview"
            });
        }

        return Ok(new
        {
            success = true,
            message = result.Message,
            data = result.Preview
        });
    }

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetById(int id)
    {
        var portfolio = await _portfolioService.GetByIdAsync(id);
        if (portfolio == null) return NotFound(new { error = $"Portfolio {id} not found" });
        if (!string.Equals(portfolio.Status, ActiveStatus, StringComparison.OrdinalIgnoreCase))
            return NotFound(new { error = $"Portfolio {id} not found" });

        var blocks = await _blockRepo.GetByPortfolioIdAsync(id);
        portfolio.Blocks = blocks.Select(b => _blockService.MapBlockToDto(b)).ToList();

        return Ok(portfolio);
    }

    [HttpGet("{id:int}/match-jobs")]
    [Authorize]
    public async Task<IActionResult> MatchJobs(
        int id,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await _portfolioService.MatchJobsForPortfolioAsync(id, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("internal/matching-candidates")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMatchingCandidates(
        [FromQuery] int limit = 150,
        CancellationToken cancellationToken = default)
    {
        var result = await _portfolioService.GetMatchingCandidatesAsync(limit, cancellationToken);
        return Ok(result);
    }

    /// <summary>Internal endpoint: batch fetch portfolio summaries by IDs</summary>
    [HttpGet("internal/by-ids")]
    [AllowAnonymous]
    public async Task<IActionResult> GetPortfoliosByIds([FromQuery] string ids)
    {
        if (string.IsNullOrWhiteSpace(ids))
            return Ok(new List<object>());

        var idList = ids.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => int.TryParse(x, out var v) ? v : (int?)null)
            .Where(x => x.HasValue && x.Value > 0)
            .Select(x => x!.Value)
            .Distinct()
            .ToList();

        if (idList.Count == 0)
            return Ok(new List<object>());

        var result = await _portfolioService.GetPortfoliosByIdsAsync(idList);
        return Ok(result);
    }

    [HttpGet("employee/{employeeId:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByEmployee(int employeeId)
    {
        var portfolios = (await _portfolioService.GetByEmployeeIdAsync(employeeId))
            .Where(p => string.Equals(p.Status, ActiveStatus, StringComparison.OrdinalIgnoreCase))
            .ToList();

        foreach (var p in portfolios)
        {
            var blocks = await _blockRepo.GetByPortfolioIdAsync(p.PortfolioId);
            p.Blocks = blocks.Select(b => _blockService.MapBlockToDto(b)).ToList();
        }

        return Ok(portfolios);
    }

    [HttpGet("employee/{employeeId:int}/main")]
    [AllowAnonymous]
    public async Task<IActionResult> GetMainByEmployee(int employeeId)
    {
        var portfolio = await _portfolioService.GetMainByEmployeeIdAsync(employeeId);
        if (portfolio == null) return NotFound(new { error = $"Main portfolio for employee {employeeId} not found" });
        if (!string.Equals(portfolio.Status, ActiveStatus, StringComparison.OrdinalIgnoreCase))
            return NotFound(new { error = $"Main portfolio for employee {employeeId} not found" });

        var blocks = await _blockRepo.GetByPortfolioIdAsync(portfolio.PortfolioId);
        portfolio.Blocks = blocks.Select(b => _blockService.MapBlockToDto(b)).ToList();

        return Ok(portfolio);
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

    [HttpPatch("{id:int}/toggle-main")]
    public async Task<IActionResult> ToggleMain(int id)
    {
        var employeeId = GetEmployeeId();
        if (employeeId == null) return Unauthorized(new { error = "EmployeeId claim not found" });

        try
        {
            var portfolio = await _portfolioService.ToggleMainAsync(id, employeeId.Value);
            return Ok(portfolio);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling main for portfolio {Id}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPatch("{id:int}/toggle-public")]
    public async Task<IActionResult> TogglePublic(int id)
    {
        var employeeId = GetEmployeeId();
        if (employeeId == null) return Unauthorized(new { error = "EmployeeId claim not found" });

        try
        {
            var portfolio = await _portfolioService.TogglePublicAsync(id, employeeId.Value);
            return Ok(portfolio);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling public for portfolio {Id}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPut("{id:int}/full")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateFull(int id, [FromForm] string portfolioJson, [FromForm] List<IFormFile>? files)
    {
        var employeeId = GetEmployeeId();
        if (employeeId == null) return Unauthorized(new { error = "EmployeeId claim not found" });

        UpdateFullPortfolioRequest request;
        try
        {
            request = System.Text.Json.JsonSerializer.Deserialize<UpdateFullPortfolioRequest>(
                portfolioJson,
                new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? throw new ArgumentException("portfolioJson cannot be null");
        }
        catch (System.Text.Json.JsonException ex)
        {
            return BadRequest(new { error = $"Invalid JSON: {ex.Message}" });
        }

        var fileMap = Request.Form.Files
            .GroupBy(f => Path.GetFileName(f.FileName))
            .ToDictionary(g => g.Key, g => g.First());

        try
        {
            var result = await _portfolioService.UpdateFullPortfolioAsync(id, employeeId.Value, request, fileMap);
            return Ok(result);
        }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fully updating portfolio {Id}", id);
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

    private int? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? User.FindFirst("sub")?.Value
                 ?? User.FindFirst("nameid")?.Value
                 ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
        if (!string.IsNullOrEmpty(claim) && int.TryParse(claim, out int userId))
            return userId;

        return null;
    }

    /// <summary>Report a portfolio</summary>
    [HttpPost("{portfolioId:int}/report")]
    [Authorize]
    public async Task<IActionResult> ReportPortfolio(int portfolioId, [FromBody] CreatePortfolioReportRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized(new { error = "Invalid token" });

        try
        {
            var created = await _portfolioService.ReportPortfolioAsync(portfolioId, userId.Value, request);
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

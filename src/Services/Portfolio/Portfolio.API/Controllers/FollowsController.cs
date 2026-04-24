using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;

namespace Portfolio.API.Controllers;

[ApiController]
[Route("api/follows")]
[Authorize(Roles = "RECRUITER")]
public class FollowsController : ControllerBase
{
    private readonly IPortfolioFollowService _followService;
    private readonly ILogger<FollowsController> _logger;

    public FollowsController(IPortfolioFollowService followService, ILogger<FollowsController> logger)
    {
        _followService = followService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePortfolioFollowRequest request)
    {
        try
        {
            var result = await _followService.CreateAsync(request);
            return StatusCode(201, result);
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating follow for portfolio {PortfolioId}", request.PortfolioId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet]
    public async Task<IActionResult> GetMyFollows([FromQuery] int? categoryId = null)
    {
        try
        {
            var result = await _followService.GetMyFollowsAsync(categoryId);
            return Ok(result);
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting followed portfolios");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPut("{portfolioId:int}")]
    public async Task<IActionResult> UpdateInterest(int portfolioId, [FromBody] UpdatePortfolioFollowRequest request)
    {
        try
        {
            var result = await _followService.UpdateInterestAsync(portfolioId, request);
            return Ok(result);
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating follow for portfolio {PortfolioId}", portfolioId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpDelete("{portfolioId:int}")]
    public async Task<IActionResult> Delete(int portfolioId)
    {
        try
        {
            await _followService.DeleteAsync(portfolioId);
            return NoContent();
        }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting follow for portfolio {PortfolioId}", portfolioId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

using System.Security.Claims;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;

namespace Portfolio.API.Controllers;

[ApiController]
[Route("api/portfolio/admin")]
[Authorize(Roles = "ADMIN,MODERATOR")]
public class AdminPortfolioModerationController : ControllerBase
{
    private readonly IPortfolioService _portfolioService;

    public AdminPortfolioModerationController(IPortfolioService portfolioService)
    {
        _portfolioService = portfolioService;
    }

    [HttpGet("pending")]
    public async Task<IActionResult> GetPending([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await _portfolioService.GetPendingPortfoliosAsync(page, pageSize);
        return Ok(result);
    }

    [HttpPost("{id:int}/approve")]
    public async Task<IActionResult> Approve(int id, [FromBody] ApprovePortfolioRequest? request)
    {
        var reviewerId = GetReviewerId();
        if (reviewerId == null)
        {
            return Unauthorized(new { error = "UserId claim not found" });
        }

        try
        {
            var actorRole = ResolveActorRole();
            var result = await _portfolioService.ApprovePortfolioAsync(id, reviewerId.Value, actorRole, request?.Notes);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost("{id:int}/reject")]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectPortfolioRequest request)
    {
        var reviewerId = GetReviewerId();
        if (reviewerId == null)
        {
            return Unauthorized(new { error = "UserId claim not found" });
        }

        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return BadRequest(new { error = "Reason is required" });
        }

        try
        {
            var actorRole = ResolveActorRole();
            var result = await _portfolioService.RejectPortfolioAsync(id, reviewerId.Value, actorRole, request.Reason);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    private int? GetReviewerId()
    {
        var claimValue = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                         ?? User.FindFirst("sub")?.Value
                         ?? User.FindFirst("userId")?.Value
                         ?? User.FindFirst("UserId")?.Value;

        return int.TryParse(claimValue, out var userId) ? userId : null;
    }

    private string ResolveActorRole()
    {
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value);
        return roles.Any(r => string.Equals(r, "MODERATOR", StringComparison.OrdinalIgnoreCase))
            ? "MODERATOR"
            : "ADMIN";
    }
}

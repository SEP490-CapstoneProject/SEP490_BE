using Challenge.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Challenge.API.Controllers;

[ApiController]
[Route("api/portfolio/skills")]
[Authorize]
public class PortfolioSkillsControllerV2 : ControllerBase
{
    private readonly IPortfolioSkillsService _portfolioSkillsService;
    private readonly ILogger<PortfolioSkillsControllerV2> _logger;

    public PortfolioSkillsControllerV2(
        IPortfolioSkillsService portfolioSkillsService,
        ILogger<PortfolioSkillsControllerV2> logger)
    {
        _portfolioSkillsService = portfolioSkillsService;
        _logger = logger;
    }

    [HttpGet("{userId:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVerifiedSkills(int userId, CancellationToken cancellationToken)
    {
        var result = await _portfolioSkillsService.GetVerifiedSkillsForPortfolioAsync(userId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{userId:int}/history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSkillHistory(int userId, [FromQuery] Guid? skillId, CancellationToken cancellationToken)
    {
        var result = await _portfolioSkillsService.GetSkillHistoryAsync(userId, skillId, cancellationToken);
        return Ok(result);
    }

    [HttpGet("leaderboard")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLeaderboard([FromQuery] int limit = 10, [FromQuery] string verificationLevel = "Expert", CancellationToken cancellationToken = default)
    {
        var result = await _portfolioSkillsService.GetSkillLeaderboardAsync(limit, verificationLevel, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{userId:int}/statistics")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSkillStatistics(int userId, CancellationToken cancellationToken)
    {
        var result = await _portfolioSkillsService.GetUserSkillStatisticsAsync(userId, cancellationToken);
        return Ok(result);
    }
}

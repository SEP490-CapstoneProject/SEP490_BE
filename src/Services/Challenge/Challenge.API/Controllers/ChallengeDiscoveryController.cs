using Challenge.Application.DTOs;
using Challenge.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Challenge.API.Controllers;

/// <summary>
/// Public challenge discovery API for participants
/// </summary>
[ApiController]
[Route("api/challenges/public")]
[AllowAnonymous]
public class ChallengeDiscoveryController : ControllerBase
{
    private readonly IChallengeService _challengeService;
    private readonly ILogger<ChallengeDiscoveryController> _logger;

    public ChallengeDiscoveryController(
        IChallengeService challengeService,
        ILogger<ChallengeDiscoveryController> logger)
    {
        _challengeService = challengeService;
        _logger = logger;
    }

    /// <summary>
    /// Get published challenges available for participants
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublishedChallenges(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? skillFilter = null)
    {
        try
        {
            var (items, totalCount) = await _challengeService.GetPublishedChallengesAsync(
                skip, 
                take, 
                search, 
                skillFilter);

            return Ok(new
            {
                items,
                totalCount,
                skip,
                take
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting published challenges");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get a specific published challenge by ID
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PublicChallengeDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPublicChallenge(Guid id)
    {
        try
        {
            var challenge = await _challengeService.GetPublicChallengeByIdAsync(id);
            if (challenge is null)
                return NotFound(new { message = $"Published challenge {id} not found" });

            return Ok(challenge);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting published challenge {Id}", id);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
        }
    }
}

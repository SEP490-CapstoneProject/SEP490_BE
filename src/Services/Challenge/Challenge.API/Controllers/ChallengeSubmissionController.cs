using Challenge.Application.DTOs;
using Challenge.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Challenge.Application.Helpers;

namespace Challenge.API.Controllers;

/// <summary>
/// Submission management for challenge participants
/// </summary>
[ApiController]
[Route("api/challenges")]
[Authorize]
public class ChallengeSubmissionController : ControllerBase
{
    private readonly ISubmissionService _submissionService;
    private readonly ILogger<ChallengeSubmissionController> _logger;

    public ChallengeSubmissionController(
        ISubmissionService submissionService,
        ILogger<ChallengeSubmissionController> logger)
    {
        _submissionService = submissionService;
        _logger = logger;
    }

    private int? GetCurrentUserId() => ClaimExtractor.GetUserId(User);

    /// <summary>
    /// Get participant's submissions for a specific challenge
    /// </summary>
    [HttpGet("{challengeId:guid}/my-submissions")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMySubmissions(
        Guid challengeId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        try
        {
            var result = await _submissionService.GetUserSubmissionsForChallengeAsync(
                challengeId,
                userId.Value,
                skip,
                take);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Challenge {ChallengeId} not found", challengeId);
            return NotFound(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Challenge {ChallengeId} not published", challengeId);
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting submissions for user {UserId} in challenge {ChallengeId}",
                userId, challengeId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
        }
    }
}

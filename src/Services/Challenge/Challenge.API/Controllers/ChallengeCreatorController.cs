using Challenge.Application.DTOs;
using Challenge.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Challenge.Application.Helpers;

namespace Challenge.API.Controllers;

/// <summary>
/// Challenge creator management API
/// </summary>
[ApiController]
[Route("api/creator/challenges")]
[Authorize]
public class ChallengeCreatorController : ControllerBase
{
    private readonly IChallengeService _challengeService;
    private readonly ISubmissionService _submissionService;
    private readonly ILogger<ChallengeCreatorController> _logger;

    public ChallengeCreatorController(
        IChallengeService challengeService,
        ISubmissionService submissionService,
        ILogger<ChallengeCreatorController> logger)
    {
        _challengeService = challengeService;
        _submissionService = submissionService;
        _logger = logger;
    }

    private int? GetCurrentUserId() => ClaimExtractor.GetUserId(User);

    /// <summary>
    /// Get all challenges created by the current user
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyChallenges(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        try
        {
            var (items, totalCount) = await _challengeService.GetCreatorChallengesAsync(userId.Value, skip, take);
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
            _logger.LogError(ex, "Error getting creator challenges for user {UserId}", userId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get all versions of a specific challenge
    /// </summary>
    [HttpGet("{challengeId:guid}/versions")]
    [ProducesResponseType(typeof(List<ChallengeVersionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetChallengeVersions(Guid challengeId)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        try
        {
            var versions = await _challengeService.GetChallengeVersionsAsync(challengeId, userId.Value);
            return Ok(versions);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting versions for challenge {ChallengeId}", challengeId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Set a specific version as the active version for a challenge
    /// </summary>
    [HttpPut("{challengeId:guid}/versions/{versionId:guid}")]
    [ProducesResponseType(typeof(ChallengeVersionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetActiveVersion(Guid challengeId, Guid versionId)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        try
        {
            var version = await _challengeService.SetActiveVersionAsync(challengeId, versionId, userId.Value);
            return Ok(version);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting active version {VersionId} for challenge {ChallengeId}", versionId, challengeId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Approve and publish a challenge (creator self-approval)
    /// Only works when challenge status is PendingReview
    /// </summary>
    [HttpPost("{challengeId:guid}/approve-and-publish")]
    [ProducesResponseType(typeof(CreatorChallengeDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ApproveAndPublish(Guid challengeId)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        try
        {
            var challenge = await _challengeService.ApproveAndPublishAsync(challengeId, userId.Value);
            return Ok(challenge);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error approving and publishing challenge {ChallengeId}", challengeId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get all submissions for a specific challenge
    /// Only the challenge creator can view submissions
    /// </summary>
    [HttpGet("{challengeId:guid}/submissions")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetChallengeSubmissions(
        Guid challengeId,
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        try
        {
            // Verify user is the challenge creator
            var challenge = await _challengeService.GetChallengeByIdAsync(challengeId, userId.Value);
            if (challenge is null)
                return Forbid();

            // Get submissions with user info
            var result = await _submissionService.GetChallengeSubmissionsWithUserInfoAsync(
                challengeId,
                skip,
                take);

            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Challenge {ChallengeId} not found", challengeId);
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting submissions for challenge {ChallengeId}", challengeId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
        }
    }
}

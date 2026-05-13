using Challenge.Application.DTOs;
using Challenge.Application.Helpers;
using Challenge.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Challenge.API.Controllers;

/// <summary>
/// Refactored Challenge controller following Community service patterns
/// </summary>
[ApiController]
[Route("api/challenges")]
[Authorize]
public class ChallengeControllerV2 : ControllerBase
{
    private readonly IChallengeService _challengeService;
    private readonly ILogger<ChallengeControllerV2> _logger;

    public ChallengeControllerV2(
        IChallengeService challengeService,
        ILogger<ChallengeControllerV2> logger)
    {
        _challengeService = challengeService;
        _logger = logger;
    }

    private int? GetCurrentUserId() => ClaimExtractor.GetUserId(User);
    private bool IsAdmin() => ClaimExtractor.IsAdmin(User);

    /// <summary>
    /// Create a new challenge (Draft status)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ChallengeDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateChallenge([FromBody] CreateChallengeDto request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        try
        {
            var challenge = await _challengeService.CreateChallengeAsync(request, userId.Value);
            return CreatedAtAction(nameof(GetChallenge), new { id = challenge.Id }, challenge);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating challenge");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get challenge by ID
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ChallengeDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetChallenge(int id)
    {
        var userId = GetCurrentUserId();
        var challenge = await _challengeService.GetChallengeByIdAsync(id, userId);
        
        if (challenge == null)
            return NotFound(new { message = $"Challenge {id} not found" });

        return Ok(challenge);
    }

    /// <summary>
    /// List all challenges with pagination
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<ChallengeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListChallenges(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        [FromQuery] string? status = null,
        [FromQuery] int? userId = null)
    {
        var (items, totalCount) = await _challengeService.GetChallengesPagedAsync(skip, take, status, userId);
        
        return Ok(new
        {
            items,
            totalCount,
            skip,
            take
        });
    }

    /// <summary>
    /// Update challenge
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ChallengeDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateChallenge(int id, [FromBody] UpdateChallengeDto request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        try
        {
            var challenge = await _challengeService.UpdateChallengeAsync(id, request, userId.Value);
            return Ok(challenge);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Forbid();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Challenge {id} not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating challenge");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete challenge
    /// </summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteChallenge(int id)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        try
        {
            await _challengeService.DeleteChallengeAsync(id, userId.Value);
            return NoContent();
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Challenge {id} not found" });
        }
    }

    /// <summary>
    /// Submit challenge for review
    /// </summary>
    [HttpPost("{id:int}/submit-review")]
    [ProducesResponseType(typeof(ChallengeDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SubmitForReview(int id)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        try
        {
            var challenge = await _challengeService.SubmitForReviewAsync(id, userId.Value);
            return Ok(challenge);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Challenge {id} not found" });
        }
    }

    /// <summary>
    /// [Admin] Approve challenge
    /// </summary>
    [HttpPost("{id:int}/approve")]
    [Authorize(Roles = "Admin,ADMIN")]
    [ProducesResponseType(typeof(ChallengeDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ApproveChallenge(int id)
    {
        var adminId = GetCurrentUserId();
        if (!adminId.HasValue || !IsAdmin())
            return Forbid();

        try
        {
            var challenge = await _challengeService.ApproveChallengeAsync(id, adminId.Value);
            return Ok(challenge);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Challenge {id} not found" });
        }
    }

    /// <summary>
    /// [Admin] Reject challenge
    /// </summary>
    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = "Admin,ADMIN")]
    [ProducesResponseType(typeof(ChallengeDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> RejectChallenge(int id, [FromBody] RejectChallengeDto request)
    {
        var adminId = GetCurrentUserId();
        if (!adminId.HasValue || !IsAdmin())
            return Forbid();

        try
        {
            var challenge = await _challengeService.RejectChallengeAsync(id, request.Reason, adminId.Value);
            return Ok(challenge);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Challenge {id} not found" });
        }
    }
}

public class RejectChallengeDto
{
    public string Reason { get; set; } = "";
}

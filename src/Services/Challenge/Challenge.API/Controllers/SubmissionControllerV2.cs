using Challenge.Application.DTOs;
using Challenge.Application.Helpers;
using Challenge.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Challenge.API.Controllers;

/// <summary>
/// Refactored Submission controller for managing submissions and grading
/// </summary>
[ApiController]
[Route("api/submissions")]
[Authorize]
public class SubmissionControllerV2 : ControllerBase
{
    private readonly ISubmissionService _submissionService;
    private readonly ILogger<SubmissionControllerV2> _logger;

    public SubmissionControllerV2(
        ISubmissionService submissionService,
        ILogger<SubmissionControllerV2> logger)
    {
        _submissionService = submissionService;
        _logger = logger;
    }

    private int? GetCurrentUserId() => ClaimExtractor.GetUserId(User);

    /// <summary>
    /// Submit a solution to a challenge
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SubmissionDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> SubmitSolution(
        [FromQuery] int challengeId,
        [FromBody] SubmitSolutionDto request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        try
        {
            var submission = await _submissionService.SubmitSolutionAsync(challengeId, request, userId.Value);
            return CreatedAtAction(nameof(GetSubmission), new { id = submission.Id }, submission);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting solution");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get submission by ID
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SubmissionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubmission(int id)
    {
        var userId = GetCurrentUserId();
        var submission = await _submissionService.GetSubmissionByIdAsync(id, userId);

        if (submission == null)
            return NotFound(new { message = $"Submission {id} not found" });

        return Ok(submission);
    }

    /// <summary>
    /// Get user's submissions
    /// </summary>
    [HttpGet("user/{userId:int}")]
    [ProducesResponseType(typeof(List<SubmissionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserSubmissions(int userId, [FromQuery] int? challengeId = null)
    {
        var currentUserId = GetCurrentUserId();
        
        // Users can only see their own submissions unless admin
        if (currentUserId != userId && !ClaimExtractor.IsAdmin(User))
            return Forbid();

        var submissions = await _submissionService.GetUserSubmissionsAsync(userId, challengeId);
        return Ok(submissions);
    }

    /// <summary>
    /// Get all submissions for a challenge
    /// </summary>
    [HttpGet("challenge/{challengeId:int}")]
    [Authorize(Roles = "Admin,ADMIN")]
    [ProducesResponseType(typeof(List<SubmissionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetChallengeSubmissions(int challengeId)
    {
        var submissions = await _submissionService.GetChallengeSubmissionsAsync(challengeId);
        return Ok(submissions);
    }

    /// <summary>
    /// Grade a submission (AI grading)
    /// </summary>
    [HttpPost("{id:int}/grade")]
    [Authorize(Roles = "Admin,ADMIN")]
    [ProducesResponseType(typeof(SubmissionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GradeSubmission(int id)
    {
        try
        {
            var submission = await _submissionService.GradeSubmissionAsync(id);
            return Ok(submission);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error grading submission {id}");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
        }
    }

    /// <summary>
    /// List submissions with pagination
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,ADMIN")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListSubmissions(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 20,
        [FromQuery] string? status = null)
    {
        var (items, totalCount) = await _submissionService.GetSubmissionsPagedAsync(skip, take, status);
        
        return Ok(new
        {
            items,
            totalCount,
            skip,
            take
        });
    }
}

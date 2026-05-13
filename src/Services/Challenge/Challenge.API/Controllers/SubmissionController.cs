using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Challenge.Application.DTOs;
using Challenge.Domain.Repositories;
using Challenge.Domain.Entities;
using Challenge.Domain.Enums;
namespace Challenge.API.Controllers;


[ApiController]
[Route("api/challenges/{challengeId}/submissions")]
[Authorize]
public class SubmissionController : ControllerBase
{
    private readonly ISubmissionRepository _submissionRepository;
    private readonly IChallengeRepository _challengeRepository;
    private readonly ILogger<SubmissionController> _logger;

    public SubmissionController(
        ISubmissionRepository submissionRepository,
        IChallengeRepository challengeRepository,
        ILogger<SubmissionController> logger)
    {
        _submissionRepository = submissionRepository;
        _challengeRepository = challengeRepository;
        _logger = logger;
    }

    /// <summary>
    /// Submit solution to challenge
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SubmissionResponseDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SubmitChallenge(
        Guid challengeId,
        [FromBody] SubmitChallengeDto dto,
        CancellationToken cancellationToken = default)
    {
        var challenge = await _challengeRepository.GetByIdAsync(challengeId, cancellationToken);
        if (challenge == null)
            return NotFound("Challenge not found");

        if (challenge.Status != ChallengeStatus.Published)
            return BadRequest("Challenge is not accepting submissions");

        var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
        var attemptCount = await _submissionRepository.GetAttemptCountAsync(userId, challengeId, cancellationToken);

        var submission = new ChallengeSubmission
        {
            Id = Guid.NewGuid(),
            ChallengeId = challengeId,
            UserId = userId,
            SubmissionContent = dto.SubmissionContent,
            GithubUrl = dto.GithubUrl,
            Status = SubmissionStatus.Pending,
            VersionSnapshotId = challenge.CurrentVersionId,
            AttemptCount = attemptCount + 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _submissionRepository.AddAsync(submission, cancellationToken);
        _logger.LogInformation("Submission created: {SubmissionId} for challenge {ChallengeId}", submission.Id, challengeId);

        return CreatedAtAction(nameof(GetSubmission), 
            new { challengeId, id = submission.Id }, 
            MapToDto(submission));
    }

    /// <summary>
    /// Get submission details with grading results
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SubmissionResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubmission(
        Guid challengeId,
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var submission = await _submissionRepository.GetByIdAsync(id, cancellationToken);
        if (submission == null || submission.ChallengeId != challengeId)
            return NotFound();

        return Ok(MapToDto(submission));
    }

    /// <summary>
    /// List user's submissions for a challenge
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SubmissionResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListSubmissions(
        Guid challengeId,
        CancellationToken cancellationToken = default)
    {
        var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
        var submissions = await _submissionRepository.GetByUserAndChallengeAsync(userId, challengeId, cancellationToken);
        var dtos = submissions.Select(MapToDto);

        return Ok(dtos);
    }

    private static SubmissionResponseDto MapToDto(ChallengeSubmission submission)
    {
        return new SubmissionResponseDto
        {
            Id = submission.Id,
            ChallengeId = submission.ChallengeId,
            OverallScore = submission.OverallScore,
            AiFeedback = submission.AiFeedback,
            Status = submission.Status,
            AttemptCount = submission.AttemptCount,
            CreatedAt = submission.CreatedAt,
            GradedAt = submission.GradedAt
        };
    }
}

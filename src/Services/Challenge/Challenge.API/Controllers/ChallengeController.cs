using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Challenge.Application.DTOs;
using Challenge.Domain.Repositories;
using Challenge.Domain.Entities;
using Challenge.Domain.Enums;
using ChallengeEntity = Challenge.Domain.Entities.Challenge;
namespace Challenge.API.Controllers;


[ApiController]
[Route("api/legacy/challenges")]
[ApiExplorerSettings(IgnoreApi = true)]
[Authorize]
public class ChallengeController : ControllerBase
{
    private readonly IChallengeRepository _challengeRepository;
    private readonly IChallengeVersionRepository _versionRepository;
    private readonly ILogger<ChallengeController> _logger;

    public ChallengeController(
        IChallengeRepository challengeRepository,
        IChallengeVersionRepository versionRepository,
        ILogger<ChallengeController> logger)
    {
        _challengeRepository = challengeRepository;
        _versionRepository = versionRepository;
        _logger = logger;
    }

    /// <summary>
    /// Create a new challenge (Draft status)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ChallengeDtoResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateChallenge(
        [FromBody] CreateChallengeDto dto,
        CancellationToken cancellationToken = default)
    {
        var challenge = new ChallengeEntity
        {
            Id = Guid.NewGuid(),
            Title = dto.Title,
            Description = dto.Description,
            ExpectedSolution = dto.ExpectedSolution,
            Status = ChallengeStatus.Draft,
            Deadline = dto.Deadline,
            CreatedById = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString()),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _challengeRepository.AddAsync(challenge, cancellationToken);

        var response = MapToDto(challenge);
        return CreatedAtAction(nameof(GetChallenge), new { id = challenge.Id }, response);
    }

    /// <summary>
    /// Get challenge by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ChallengeDtoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetChallenge(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var challenge = await _challengeRepository.GetByIdAsync(id, cancellationToken);
        if (challenge == null)
            return NotFound();

        return Ok(MapToDto(challenge));
    }

    /// <summary>
    /// List all published challenges
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ChallengeDtoResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListChallenges(
        CancellationToken cancellationToken = default)
    {
        var challenges = await _challengeRepository.GetPublishedAsync(cancellationToken);
        var dtos = challenges.Select(MapToDto);
        return Ok(dtos);
    }

    /// <summary>
    /// Update challenge (Draft only)
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ChallengeDtoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateChallenge(
        Guid id,
        [FromBody] UpdateChallengeDto dto,
        CancellationToken cancellationToken = default)
    {
        var challenge = await _challengeRepository.GetByIdAsync(id, cancellationToken);
        if (challenge == null)
            return NotFound();

        if (challenge.Status != ChallengeStatus.Draft)
            return BadRequest("Can only edit challenges in Draft status");

        challenge.Title = dto.Title;
        challenge.Description = dto.Description;
        challenge.ExpectedSolution = dto.ExpectedSolution;
        challenge.Deadline = dto.Deadline;
        challenge.UpdatedAt = DateTime.UtcNow;

        await _challengeRepository.UpdateAsync(challenge, cancellationToken);

        return Ok(MapToDto(challenge));
    }

    /// <summary>
    /// Delete challenge (Draft only)
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteChallenge(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var challenge = await _challengeRepository.GetByIdAsync(id, cancellationToken);
        if (challenge == null)
            return NotFound();

        if (challenge.Status != ChallengeStatus.Draft)
            return BadRequest("Can only delete challenges in Draft status");

        await _challengeRepository.DeleteAsync(id, cancellationToken);

        return NoContent();
    }

    /// <summary>
    /// Submit challenge for review (Admin only)
    /// </summary>
    [HttpPost("{id}/submit-review")]
    [ProducesResponseType(typeof(ChallengeDtoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitForReview(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var challenge = await _challengeRepository.GetByIdAsync(id, cancellationToken);
        if (challenge == null)
            return NotFound();

        if (challenge.Status != ChallengeStatus.Draft)
            return BadRequest("Can only submit Draft challenges for review");

        challenge.Status = ChallengeStatus.PendingReview;
        challenge.UpdatedAt = DateTime.UtcNow;

        await _challengeRepository.UpdateAsync(challenge, cancellationToken);

        return Ok(MapToDto(challenge));
    }

    /// <summary>
    /// Approve challenge (Admin only) - Creates snapshot
    /// </summary>
    [HttpPost("{id}/approve")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ChallengeDtoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> ApproveChallenge(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var challenge = await _challengeRepository.GetByIdAsync(id, cancellationToken);
        if (challenge == null)
            return NotFound();

        if (challenge.Status != ChallengeStatus.PendingReview)
            return BadRequest("Can only approve challenges in PendingReview status");

        // Create immutable snapshot (ChallengeVersion)
        var version = new ChallengeVersion
        {
            Id = Guid.NewGuid(),
            ChallengeId = id,
            VersionNumber = 1,
            Title = challenge.Title,
            Description = challenge.Description,
            ExpectedSolution = challenge.ExpectedSolution,
            DifficultyScore = challenge.DifficultyScore,
            DifficultyLabel = challenge.DifficultyLabel,
            SkillWeightMapping = "{}", // To be populated by AI analysis
            ModelName = "PendingGeminiAnalysis",
            PromptVersion = "v1.0",
            EvaluatedAt = DateTime.UtcNow,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _versionRepository.AddAsync(version, cancellationToken);

        // Update challenge
        challenge.Status = ChallengeStatus.Published;
        challenge.CurrentVersionId = version.Id;
        challenge.ReviewedById = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
        challenge.PublishedAt = DateTime.UtcNow;
        challenge.UpdatedAt = DateTime.UtcNow;

        await _challengeRepository.UpdateAsync(challenge, cancellationToken);

        _logger.LogInformation("Challenge {ChallengeId} approved and published with version {VersionId}", id, version.Id);

        return Ok(MapToDto(challenge));
    }

    /// <summary>
    /// Reject challenge (Admin only)
    /// </summary>
    [HttpPost("{id}/reject")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ChallengeDtoResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> RejectChallenge(
        Guid id,
        [FromBody] ModerateChallengeDto dto,
        CancellationToken cancellationToken = default)
    {
        var challenge = await _challengeRepository.GetByIdAsync(id, cancellationToken);
        if (challenge == null)
            return NotFound();

        if (challenge.Status != ChallengeStatus.PendingReview)
            return BadRequest("Can only reject challenges in PendingReview status");

        challenge.Status = ChallengeStatus.Rejected;
        challenge.RejectionReason = dto.RejectionReason;
        challenge.ReviewedById = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());
        challenge.UpdatedAt = DateTime.UtcNow;

        await _challengeRepository.UpdateAsync(challenge, cancellationToken);

        _logger.LogInformation("Challenge {ChallengeId} rejected: {Reason}", id, dto.RejectionReason);

        return Ok(MapToDto(challenge));
    }

    private static ChallengeDtoResponse MapToDto(ChallengeEntity challenge)
    {
        return new ChallengeDtoResponse
        {
            Id = challenge.Id,
            Title = challenge.Title,
            Description = challenge.Description,
            DifficultyScore = challenge.DifficultyScore,
            DifficultyLabel = challenge.DifficultyLabel,
            Status = challenge.Status,
            Deadline = challenge.Deadline,
            PublishedAt = challenge.PublishedAt,
            CreatedAt = challenge.CreatedAt,
            RejectionReason = challenge.RejectionReason
        };
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Challenge.Application.DTOs;
using Challenge.Domain.Repositories;
using Challenge.Domain.Enums;
namespace Challenge.API.Controllers;


[ApiController]
[Route("api/portfolio")]
[Authorize]
public class PortfolioSkillsController : ControllerBase
{
    private readonly IUserSkillRepository _userSkillRepo;
    private readonly ISkillPointTransactionRepository _transactionRepo;
    private readonly ILogger<PortfolioSkillsController> _logger;

    public PortfolioSkillsController(
        IUserSkillRepository userSkillRepo,
        ISkillPointTransactionRepository transactionRepo,
        ILogger<PortfolioSkillsController> logger)
    {
        _userSkillRepo = userSkillRepo;
        _transactionRepo = transactionRepo;
        _logger = logger;
    }

    /// <summary>
    /// Get all AI-verified skills for a user
    /// </summary>
    [HttpGet("verified-skills")]
    [ProducesResponseType(typeof(IEnumerable<VerifiedSkillDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVerifiedSkills(CancellationToken cancellationToken = default)
    {
        var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());

        var verifiedSkills = await _userSkillRepo.GetVerifiedByUserAsync(userId, cancellationToken);
        var dtos = verifiedSkills.Select(s => new VerifiedSkillDto
        {
            SkillId = s.SkillId,
            SkillName = s.SkillId.ToString(), // Would need skill repository for actual name
            TotalPoints = s.TotalPoints,
            MasteryScore = s.MasteryScore,
            VerifiedChallengeCount = s.VerifiedChallengeCount,
            VerificationLevel = s.VerificationLevel,
            LastVerifiedAt = s.LastVerifiedAt ?? DateTime.MinValue
        }).OrderByDescending(s => s.TotalPoints);

        return Ok(dtos);
    }

    /// <summary>
    /// Get skill history for portfolio
    /// </summary>
    [HttpGet("verified-skills/{skillId}/history")]
    [ProducesResponseType(typeof(IEnumerable<SkillHistoryItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSkillHistory(
        Guid skillId,
        CancellationToken cancellationToken = default)
    {
        var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());

        var transactions = await _transactionRepo.GetByUserAndSkillAsync(userId, skillId, cancellationToken);
        var dtos = transactions.Select(t => new SkillHistoryItemDto
        {
            TransactionId = t.Id,
            ChallengeTitle = t.SourceId.ToString(), // Would need to load challenge title
            PointsAwarded = t.Points,
            EarnedAt = t.CreatedAt
        }).OrderByDescending(d => d.EarnedAt);

        return Ok(dtos);
    }

    /// <summary>
    /// Get user's total skill points (for leaderboard)
    /// </summary>
    [HttpGet("skill-stats")]
    [ProducesResponseType(typeof(UserSkillStatsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSkillStats(CancellationToken cancellationToken = default)
    {
        var userId = Guid.Parse(User.FindFirst("sub")?.Value ?? Guid.Empty.ToString());

        var totalPoints = await _userSkillRepo.GetTotalPointsByUserAsync(userId, cancellationToken);
        var verifiedSkills = await _userSkillRepo.GetVerifiedByUserAsync(userId, cancellationToken);

        var dto = new UserSkillStatsDto
        {
            TotalPoints = totalPoints,
            VerifiedChallengeCount = verifiedSkills.Sum(s => s.VerifiedChallengeCount),
            SkillCount = verifiedSkills.Count(),
            ExpertCount = verifiedSkills.Count(s => s.VerificationLevel == VerificationLevel.Expert),
            AdvancedCount = verifiedSkills.Count(s => s.VerificationLevel == VerificationLevel.Advanced),
            IntermediateCount = verifiedSkills.Count(s => s.VerificationLevel == VerificationLevel.Intermediate),
            BeginnerCount = verifiedSkills.Count(s => s.VerificationLevel == VerificationLevel.Beginner)
        };

        return Ok(dto);
    }
}

public class UserSkillStatsDto
{
    public decimal TotalPoints { get; set; }
    public int VerifiedChallengeCount { get; set; }
    public int SkillCount { get; set; }
    public int ExpertCount { get; set; }
    public int AdvancedCount { get; set; }
    public int IntermediateCount { get; set; }
    public int BeginnerCount { get; set; }
}

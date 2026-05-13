using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Challenge.Application.DTOs;
using Challenge.Domain.Repositories;
using Challenge.Domain.Entities;
namespace Challenge.API.Controllers;


[ApiController]
[Route("api/legacy/skills")]
[ApiExplorerSettings(IgnoreApi = true)]
[Authorize]
public class SkillController : ControllerBase
{
    private readonly ISkillRepository _skillRepository;
    private readonly ILogger<SkillController> _logger;

    public SkillController(
        ISkillRepository skillRepository,
        ILogger<SkillController> logger)
    {
        _skillRepository = skillRepository;
        _logger = logger;
    }

    /// <summary>
    /// Get all skills
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SkillDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSkills(CancellationToken cancellationToken = default)
    {
        var skills = await _skillRepository.GetAllAsync(cancellationToken);
        var dtos = skills.Select(MapToDto);
        return Ok(dtos);
    }

    /// <summary>
    /// Get skill by ID
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(SkillDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSkill(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var skill = await _skillRepository.GetByIdAsync(id, cancellationToken);
        if (skill == null)
            return NotFound();

        return Ok(MapToDto(skill));
    }

    /// <summary>
    /// Search skills by name
    /// </summary>
    [HttpGet("search/{query}")]
    [ProducesResponseType(typeof(IEnumerable<SkillDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchSkills(
        string query,
        CancellationToken cancellationToken = default)
    {
        var skill = await _skillRepository.GetByNameAsync(query, cancellationToken);
        if (skill != null)
            return Ok(new[] { MapToDto(skill) });

        // Fuzzy search
        var skills = await _skillRepository.FuzzySearchAsync(query, 0.75m, cancellationToken);
        var dtos = skills.Select(MapToDto);

        return Ok(dtos);
    }

    /// <summary>
    /// Create new skill
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(SkillDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSkill(
        [FromBody] CreateSkillDto dto,
        CancellationToken cancellationToken = default)
    {
        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            Slug = dto.Name.ToLowerInvariant().Replace(" ", "-"),
            CategoryId = dto.CategoryId,
            IsSystem = false,
            IsApproved = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _skillRepository.AddAsync(skill, cancellationToken);
        _logger.LogInformation("Skill created: {SkillId}", skill.Id);

        return CreatedAtAction(nameof(GetSkill), new { id = skill.Id }, MapToDto(skill));
    }

    private static SkillDto MapToDto(Skill skill)
    {
        return new SkillDto
        {
            Id = skill.Id,
            Name = skill.Name,
            Slug = skill.Slug,
            IsSystem = skill.IsSystem,
            IsApproved = skill.IsApproved
        };
    }
}

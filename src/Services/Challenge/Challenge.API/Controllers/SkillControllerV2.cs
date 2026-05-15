using Challenge.Application.DTOs;
using Challenge.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Challenge.API.Controllers;

/// <summary>
/// Refactored Skill controller for managing skills and skill verification
/// </summary>
[ApiController]
[Route("api/skills")]
[Authorize]
public class SkillControllerV2 : ControllerBase
{
    private readonly ISkillService _skillService;
    private readonly ILogger<SkillControllerV2> _logger;

    public SkillControllerV2(
        ISkillService skillService,
        ILogger<SkillControllerV2> logger)
    {
        _skillService = skillService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new skill [Admin only]
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin,ADMIN")]
    [ProducesResponseType(typeof(SkillDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateSkill([FromBody] CreateSkillDto request)
    {
        try
        {
            var skill = await _skillService.CreateSkillAsync(request);
            return CreatedAtAction(nameof(GetSkill), new { id = skill.Id }, skill);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating skill");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get skill by ID
    /// </summary>
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(SkillDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSkill(int id)
    {
        var skill = await _skillService.GetSkillByIdAsync(id);
        
        if (skill == null)
            return NotFound(new { message = $"Skill {id} not found" });

        return Ok(skill);
    }

    /// <summary>
    /// Get skill by slug
    /// </summary>
    [HttpGet("slug/{slug}")]
    [ProducesResponseType(typeof(SkillDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSkillBySlug(string slug)
    {
        var skill = await _skillService.GetSkillBySlugAsync(slug);
        
        if (skill == null)
            return NotFound(new { message = $"Skill '{slug}' not found" });

        return Ok(skill);
    }

    /// <summary>
    /// List all skills
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<SkillDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListSkills()
    {
        var skills = await _skillService.ListSkillsAsync();
        return Ok(skills);
    }

    /// <summary>
    /// Search skills (with fuzzy matching)
    /// </summary>
    [HttpGet("search")]
    [ProducesResponseType(typeof(List<SkillDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchSkills([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { message = "Search query required" });

        var skills = await _skillService.SearchSkillsAsync(q);
        return Ok(skills);
    }

    /// <summary>
    /// Update skill [Admin only]
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Admin,ADMIN")]
    [ProducesResponseType(typeof(SkillDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateSkill(int id, [FromBody] UpdateSkillDto request)
    {
        try
        {
            var skill = await _skillService.UpdateSkillAsync(id, request);
            return Ok(skill);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { message = $"Skill {id} not found" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating skill");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete skill [Admin only]
    /// </summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Admin,ADMIN")]
    public async Task<IActionResult> DeleteSkill(int id)
    {
        try
        {
            await _skillService.DeleteSkillAsync(id);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting skill");
            return StatusCode(StatusCodes.Status500InternalServerError, new { message = ex.Message });
        }
    }

    /// <summary>
    /// Get user's verified skills
    /// </summary>
    [HttpGet("user/{userId:int}/verified")]
    [ProducesResponseType(typeof(List<UserSkillDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetVerifiedSkills(int userId)
    {
        var userSkills = await _skillService.GetVerifiedSkillsAsync(userId);
        return Ok(userSkills);
    }

    /// <summary>
    /// Get user's skills by verification level
    /// </summary>
    [HttpGet("user/{userId:int}/level/{level}")]
    [ProducesResponseType(typeof(List<UserSkillDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSkillsByLevel(int userId, string level)
    {
        var validLevels = new[] { "Beginner", "Intermediate", "Advanced", "Expert" };
        if (!validLevels.Contains(level))
            return BadRequest(new { message = "Invalid verification level" });

        var userSkills = await _skillService.GetSkillsByVerificationLevelAsync(userId, level);
        return Ok(userSkills);
    }

    /// <summary>
    /// Get all user skills
    /// </summary>
    [HttpGet("user/{userId:int}")]
    [ProducesResponseType(typeof(List<UserSkillDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetUserSkills(int userId)
    {
        var userSkills = await _skillService.GetUserSkillsAsync(userId);
        return Ok(userSkills);
    }
}

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UserProfile.Application.DTOs;
using UserProfile.Application.Interfaces;

namespace UserProfile.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CompanyController : ControllerBase
{
    private readonly ICompanyService _service;
    private readonly ILogger<CompanyController> _logger;

    public CompanyController(ICompanyService service, ILogger<CompanyController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Get current user's company profile (requires JWT token)
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> GetMyProfile()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized(new { error = "Invalid or missing user ID in token" });
        }

        var company = await _service.GetByUserIdAsync(userId);
        if (company == null)
        {
            return NotFound(new { error = "Company profile not found" });
        }

        return Ok(company);
    }

    /// <summary>
    /// Get company profile by user ID (service-to-service, no auth required)
    /// </summary>
    [HttpGet("by-user/{userId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByUserId(int userId)
    {
        var company = await _service.GetByUserIdAsync(userId);
        if (company == null)
        {
            return NotFound(new { error = "Company profile not found" });
        }

        return Ok(company);
    }

    /// <summary>
    /// Get company profile by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var company = await _service.GetByIdAsync(id);
        if (company == null)
        {
            return NotFound(new { error = $"Company with ID {id} not found" });
        }

        return Ok(company);
    }

    /// <summary>
    /// Get all company profiles
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var companies = await _service.GetAllAsync();
        return Ok(companies);
    }

    /// <summary>
    /// Create company profile with optional avatar and cover image
    /// </summary>
    /// <summary>
    /// Create company profile with optional avatar and cover image
    /// </summary>
    [HttpPost]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create(
        [FromForm] CreateCompanyRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized(new { error = "Invalid or missing user ID in token" });
        }

        try
        {
            // Files are now in the request object
            var company = await _service.CreateAsync(userId, request, request.Avatar, request.CoverImage);
            return CreatedAtAction(nameof(GetById), new { id = company.Id }, company);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Update company profile with optional avatar and cover image
    /// </summary>
    /// <summary>
    /// Update company profile with optional avatar and cover image
    /// </summary>
    [HttpPut("{id}")]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Update(
        int id,
        [FromForm] UpdateCompanyRequest request)
    {
        try
        {
            // Files are now in the request object
            var company = await _service.UpdateAsync(id, request, request.Avatar, request.CoverImage);
            return Ok(company);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Delete company profile
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteAsync(id);
        if (!result)
        {
            return NotFound(new { error = $"Company with ID {id} not found" });
        }

        return NoContent();
    }
}

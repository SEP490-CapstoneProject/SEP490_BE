using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using UserProfile.Application.DTOs;
using UserProfile.Application.Interfaces;

namespace UserProfile.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EmployeeController : ControllerBase
{
    private readonly IEmployeeService _service;
    private readonly ILogger<EmployeeController> _logger;

    public EmployeeController(IEmployeeService service, ILogger<EmployeeController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>
    /// Get current user's employee profile (requires JWT token)
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

        var employee = await _service.GetByUserIdAsync(userId);
        if (employee == null)
        {
            return NotFound(new { error = "Employee profile not found" });
        }

        return Ok(employee);
    }

    /// <summary>
    /// Get employee profile by user ID (service-to-service, no auth required)
    /// </summary>
    [HttpGet("by-user/{userId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByUserId(int userId)
    {
        var employee = await _service.GetByUserIdAsync(userId);
        if (employee == null)
        {
            return NotFound(new { error = "Employee profile not found" });
        }

        return Ok(employee);
    }

    /// <summary>
    /// Get employee profile by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var employee = await _service.GetByIdAsync(id);
        if (employee == null)
        {
            return NotFound(new { error = $"Employee with ID {id} not found" });
        }

        return Ok(employee);
    }

    /// <summary>
    /// Get all employee profiles
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var employees = await _service.GetAllAsync();
        return Ok(employees);
    }

    /// <summary>
    /// Create employee profile with optional avatar and cover image
    /// </summary>
    /// <summary>
    /// Create employee profile with optional avatar and cover image
    /// </summary>
    [HttpPost]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Create(
        [FromForm] CreateEmployeeRequest request)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized(new { error = "Invalid or missing user ID in token" });
        }

        try
        {
            // Files are now in the request object
            var employee = await _service.CreateAsync(userId, request, request.Avatar, request.CoverImage);
            return CreatedAtAction(nameof(GetById), new { id = employee.Id }, employee);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Update employee profile with optional avatar and cover image
    /// </summary>
    /// <summary>
    /// Update employee profile with optional avatar and cover image
    /// </summary>
    [HttpPut("{id}")]
    [Authorize]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Update(
        int id,
        [FromForm] UpdateEmployeeRequest request)
    {
        try
        {
            // Files are now in the request object
            var employee = await _service.UpdateAsync(id, request, request.Avatar, request.CoverImage);
            return Ok(employee);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    /// <summary>
    /// Delete employee profile
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _service.DeleteAsync(id);
        if (!result)
        {
            return NotFound(new { error = $"Employee with ID {id} not found" });
        }

        return NoContent();
    }
}

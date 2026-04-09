using Application.Application.DTOs;
using Application.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Application.API.Controllers;

[ApiController]
[Route("api/applications")]
[Authorize]
public class ApplicationController : ControllerBase
{
    private readonly IApplicationService _service;
    private readonly ILogger<ApplicationController> _logger;

    public ApplicationController(IApplicationService service, ILogger<ApplicationController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpPost]
    [Authorize(Roles = "USER")]
    public async Task<IActionResult> Create([FromBody] CreateApplicationRequest request)
    {
        try
        {
            var result = await _service.CreateApplicationAsync(request);
            return StatusCode(201, result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating application");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("me")]
    [Authorize(Roles = "USER")]
    public async Task<IActionResult> GetMyApplications([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page < 1)
            return BadRequest(new { error = "Page must be >= 1" });
        if (pageSize < 1 || pageSize > 50)
            return BadRequest(new { error = "PageSize must be between 1 and 50" });

        try
        {
            var result = await _service.GetMyApplicationsAsync(page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting my applications");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("company")]
    [Authorize(Roles = "RECRUITER")]
    public async Task<IActionResult> GetCompanyApplications([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        if (page < 1)
            return BadRequest(new { error = "Page must be >= 1" });
        if (pageSize < 1 || pageSize > 50)
            return BadRequest(new { error = "PageSize must be between 1 and 50" });

        try
        {
            var result = await _service.GetCompanyApplicationsAsync(page, pageSize);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting company applications");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "RECRUITER")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdateApplicationStatusRequest request)
    {
        try
        {
            var result = await _service.UpdateApplicationStatusAsync(id, request);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating application status");
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        try
        {
            var result = await _service.GetApplicationByIdAsync(id);
            return Ok(result);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting application {Id}", id);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }
}

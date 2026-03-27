using Interview.Application.DTOs;
using Interview.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Interview.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InterviewController : ControllerBase
{
    private readonly IInterviewService _service;

    public InterviewController(IInterviewService service)
    {
        _service = service;
    }

    // ── GET /api/interview  (interviews of current logged-in user) ──────────
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();
        var list = await _service.GetAllByUserIdAsync(userId.Value);
        return Ok(list);
    }

    // ── GET /api/interview/{id}  (single rich DTO) ──────────────────────────
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _service.GetInterviewByIdAsync(id);
        if (item == null) return NotFound();
        return Ok(item);
    }

    // ── GET /api/interview/by-date?date=2026-03-20 ──────────────────────────
    [HttpGet("by-date")]
    public async Task<IActionResult> GetByDate([FromQuery] string date)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        if (!DateTime.TryParse(date, out var parsedDate))
            return BadRequest(new { error = "Invalid date format. Use yyyy-MM-dd." });

        var list = await _service.GetByDateAsync(userId.Value, parsedDate);
        return Ok(list);
    }

    // ── GET /api/interview/by-status?status=UPCOMING ────────────────────────
    [HttpGet("by-status")]
    public async Task<IActionResult> GetByStatus([FromQuery] string status)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var list = await _service.GetByStatusAsync(userId.Value, status);
        return Ok(list);
    }

    // ── POST /api/interview ──────────────────────────────────────────────────
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInterviewRequest req)
    {
        var interview = new Interview.Domain.Entities.Interview
        {
            ApplicationId = req.ApplicationId,
            UserId = req.UserId,
            PostId = req.PostId,
            PostPosition = req.PostPosition,
            Date = req.Date,
            Time = req.Time,
            Round = req.Round,
            Type = req.Type,
            Platform = req.Platform,
            Link = req.Link,
            Building = req.Building,
            Room = req.Room,
            Status = req.Status,
            InterviewerName = req.InterviewerName
        };

        var created = await _service.CreateInterviewAsync(interview);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    // ── PUT /api/interview/{id} ──────────────────────────────────────────────
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] CreateInterviewRequest req)
    {
        var existing = await _service.GetInterviewByIdAsync(id);
        if (existing == null) return NotFound();

        // Fetch raw entity for update
        var raw = new Interview.Domain.Entities.Interview
        {
            Id = id,
            ApplicationId = req.ApplicationId,
            UserId = req.UserId,
            PostId = req.PostId,
            PostPosition = req.PostPosition,
            Date = req.Date,
            Time = req.Time,
            Round = req.Round,
            Type = req.Type,
            Platform = req.Platform,
            Link = req.Link,
            Building = req.Building,
            Room = req.Room,
            Status = req.Status,
            InterviewerName = req.InterviewerName
        };

        await _service.UpdateInterviewAsync(raw);
        return NoContent();
    }

    // ── DELETE /api/interview/{id} ───────────────────────────────────────────
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _service.DeleteInterviewAsync(id);
        return NoContent();
    }

    // ── helpers ──────────────────────────────────────────────────────────────
    private int? GetCurrentUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? User.FindFirst("sub")?.Value
                 ?? User.FindFirst("nameid")?.Value;

        return int.TryParse(claim, out var id) ? id : null;
    }
}


using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Subscription.Application.DTOs;
using Subscription.Application.Interfaces;
using System.Security.Claims;

namespace Subscription.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<SubscriptionsController> _logger;

    public SubscriptionsController(ISubscriptionService subscriptionService, ILogger<SubscriptionsController> logger)
    {
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    [HttpPost("subscribe")]
    [Authorize]
    public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest request)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized();

        try
        {
            var subscription = await _subscriptionService.SubscribeAsync(userId.Value, request);
            return Ok(subscription);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPost("upgrade")]
    [Authorize]
    public async Task<IActionResult> Upgrade([FromBody] UpgradeRequest request)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized();

        try
        {
            var subscription = await _subscriptionService.UpgradeAsync(userId.Value, request);
            return Ok(subscription);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("cancel")]
    [Authorize]
    public async Task<IActionResult> Cancel([FromBody] CancelRequest request)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized();

        try
        {
            await _subscriptionService.CancelSubscriptionAsync(userId.Value, request);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("current")]
    [Authorize]
    public async Task<IActionResult> GetCurrentSubscription()
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized();

        var subscription = await _subscriptionService.GetActiveSubscriptionAsync(userId.Value);
        if (subscription == null)
            return NotFound(new { message = "No active subscription" });

        return Ok(subscription);
    }

    [HttpGet("entitlements/{userId}")]
    public async Task<IActionResult> GetEntitlements(int userId, [FromQuery] string? role = null)
    {
        // This endpoint is used by other services as fallback
        var entitlements = await _subscriptionService.GetEntitlementsAsync(userId, role);
        return Ok(entitlements);
    }

    [HttpGet("my-entitlements")]
    [Authorize]
    public async Task<IActionResult> GetMyEntitlements()
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized();

        // Lấy role từ JWT claim
        var role = User.FindFirst(ClaimTypes.Role)?.Value
            ?? User.FindFirst("role")?.Value;

        var entitlements = await _subscriptionService.GetEntitlementsAsync(userId.Value, role);
        return Ok(entitlements);
    }

    private int? GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
            ?? User.FindFirst("sub")?.Value
            ?? User.FindFirst("userId")?.Value;
        
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}

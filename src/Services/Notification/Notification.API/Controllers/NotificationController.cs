using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Notification.Application.Interfaces;

namespace Notification.API.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationService _service;
    private readonly IDistributedCache _cache;

    public NotificationController(INotificationService service, IDistributedCache cache)
    {
        _service = service;
        _cache = cache;
    }

    private string GetUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")?.Value
        ?? User.FindFirst("nameid")?.Value
        ?? throw new UnauthorizedAccessException("User ID not found in token");

    [HttpGet]
    public async Task<IActionResult> GetNotifications([FromQuery] int? cursor, [FromQuery] int limit = 20)
    {
        limit = Math.Clamp(limit, 1, 50);
        var userId = GetUserId();
        var result = await _service.GetNotificationsAsync(userId, cursor, limit);
        return Ok(result);
    }

    /// <summary>Get notifications related to community interactions only.</summary>
    [HttpGet("community")]
    public async Task<IActionResult> GetCommunityNotifications([FromQuery] int? cursor, [FromQuery] int limit = 20)
    {
        limit = Math.Clamp(limit, 1, 50);
        var userId = GetUserId();
        var result = await _service.GetCommunityNotificationsAsync(userId, cursor, limit);
        return Ok(result);
    }

    /// <summary>Get message-only notifications (chat messages).</summary>
    [HttpGet("message")]
    public async Task<IActionResult> GetMessageNotifications([FromQuery] int? cursor, [FromQuery] int limit = 20)
    {
        limit = Math.Clamp(limit, 1, 50);
        var userId = GetUserId();
        var result = await _service.GetMessageNotificationsAsync(userId, cursor, limit);
        return Ok(result);
    }

    /// <summary>Get system notifications excluding community-specific types.</summary>
    [HttpGet("system")]
    public async Task<IActionResult> GetSystemNotifications([FromQuery] int? cursor, [FromQuery] int limit = 20)
    {
        limit = Math.Clamp(limit, 1, 50);
        var userId = GetUserId();
        var result = await _service.GetSystemNotificationsAsync(userId, cursor, limit);
        return Ok(result);
    }

    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount()
    {
        var userId = GetUserId();
        var cacheKey = $"unread:{userId}";

        var cached = await _cache.GetStringAsync(cacheKey);
        if (cached != null)
            return Ok(new { count = int.Parse(cached) });

        var count = await _service.GetUnreadCountAsync(userId);
        await _cache.SetStringAsync(cacheKey, count.ToString(), new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        });

        return Ok(new { count });
    }

    [HttpPut("{id:int}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = GetUserId();
        await _service.MarkAsReadAsync(id, userId);
        await _cache.RemoveAsync($"unread:{userId}");
        return NoContent();
    }

    [HttpPut("read-all")]
    public async Task<IActionResult> MarkAllAsRead()
    {
        var userId = GetUserId();
        await _service.MarkAllAsReadAsync(userId);
        await _cache.RemoveAsync($"unread:{userId}");
        return NoContent();
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notification.Application.Interfaces;
using Notification.Domain.Entities;

namespace Notification.API.Controllers;

[ApiController]
[Route("api/device-tokens")]
[Authorize]
public class DeviceTokenController : ControllerBase
{
    private readonly IDeviceTokenService _deviceTokenService;
    private readonly INotificationSettingsService _settingsService;

    public DeviceTokenController(IDeviceTokenService deviceTokenService, INotificationSettingsService settingsService)
    {
        _deviceTokenService = deviceTokenService;
        _settingsService = settingsService;
    }

    private string GetUserId() =>
        User.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User.FindFirst("sub")?.Value
        ?? User.FindFirst("nameid")?.Value
        ?? throw new UnauthorizedAccessException("User ID not found in token");

    /// <summary>Register a device token for push notifications</summary>
    /// <remarks>
    /// Mobile app calls this endpoint to register its FCM device token.
    /// When the token is refreshed by Firebase, this endpoint should be called again with the new token.
    /// </remarks>
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> RegisterToken([FromBody] RegisterDeviceTokenRequest request)
    {
        var userId = GetUserId();

        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return BadRequest(new { error = "Device token is required" });
        }

        var success = await _deviceTokenService.RegisterTokenAsync(
            userId,
            request.Token,
            request.DeviceType ?? "Android",
            request.AppVersion);

        if (!success)
        {
            return StatusCode(500, new { error = "Failed to register device token" });
        }

        // Initialize notification settings if they don't exist
        var settings = await _settingsService.GetUserSettingsAsync(userId);
        if (settings == null)
        {
            await _settingsService.CreateOrUpdateSettingsAsync(new NotificationSettingsEntity
            {
                UserId = userId,
                PushNotificationsEnabled = true,
                SoundEnabled = true,
                VibrateEnabled = true,
                ChatNotificationsEnabled = true,
                MentionNotificationsEnabled = true
            });
        }

        return Ok(new { success = true, message = "Device token registered successfully" });
    }

    /// <summary>Unregister a device token (when app is uninstalled)</summary>
    /// <remarks>
    /// Mobile app should call this endpoint when the app is being uninstalled or when user logs out.
    /// </remarks>
    [HttpDelete("register/{token}")]
    public async Task<IActionResult> UnregisterToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return BadRequest(new { error = "Device token is required" });
        }

        var success = await _deviceTokenService.UnregisterTokenAsync(token);

        if (!success)
        {
            return StatusCode(500, new { error = "Failed to unregister device token" });
        }

        return Ok(new { success = true, message = "Device token unregistered successfully" });
    }

    /// <summary>Get user's notification settings</summary>
    [HttpGet("settings")]
    public async Task<IActionResult> GetNotificationSettings()
    {
        var userId = GetUserId();
        var settings = await _settingsService.GetUserSettingsAsync(userId);

        if (settings == null)
        {
            // Return default settings
            return Ok(new
            {
                pushNotificationsEnabled = true,
                soundEnabled = true,
                vibrateEnabled = true,
                chatNotificationsEnabled = true,
                mentionNotificationsEnabled = true
            });
        }

        return Ok(new
        {
            pushNotificationsEnabled = settings.PushNotificationsEnabled,
            soundEnabled = settings.SoundEnabled,
            vibrateEnabled = settings.VibrateEnabled,
            chatNotificationsEnabled = settings.ChatNotificationsEnabled,
            mentionNotificationsEnabled = settings.MentionNotificationsEnabled
        });
    }

    /// <summary>Update user's notification settings</summary>
    [HttpPut("settings")]
    public async Task<IActionResult> UpdateNotificationSettings([FromBody] UpdateNotificationSettingsRequest request)
    {
        var userId = GetUserId();

        var settings = await _settingsService.GetUserSettingsAsync(userId);

        if (settings == null)
        {
            settings = new NotificationSettingsEntity { UserId = userId };
        }

        if (request.PushNotificationsEnabled.HasValue)
            settings.PushNotificationsEnabled = request.PushNotificationsEnabled.Value;

        if (request.SoundEnabled.HasValue)
            settings.SoundEnabled = request.SoundEnabled.Value;

        if (request.VibrateEnabled.HasValue)
            settings.VibrateEnabled = request.VibrateEnabled.Value;

        if (request.ChatNotificationsEnabled.HasValue)
            settings.ChatNotificationsEnabled = request.ChatNotificationsEnabled.Value;

        if (request.MentionNotificationsEnabled.HasValue)
            settings.MentionNotificationsEnabled = request.MentionNotificationsEnabled.Value;

        await _settingsService.CreateOrUpdateSettingsAsync(settings);

        return Ok(new { success = true, message = "Notification settings updated successfully" });
    }
}

public class RegisterDeviceTokenRequest
{
    public string Token { get; set; } = "";
    public string? DeviceType { get; set; } // "Android" or "iOS"
    public string? AppVersion { get; set; }
}

public class UpdateNotificationSettingsRequest
{
    public bool? PushNotificationsEnabled { get; set; }
    public bool? SoundEnabled { get; set; }
    public bool? VibrateEnabled { get; set; }
    public bool? ChatNotificationsEnabled { get; set; }
    public bool? MentionNotificationsEnabled { get; set; }
}

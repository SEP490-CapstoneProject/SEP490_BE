using Auth.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecruitmentPlatform.Common;
using RecruitmentPlatform.Contracts.Auth;
using System.Security.Claims;
using Auth.Application.DTOs;

namespace Auth.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Register([FromBody] RegisterRequest request)
    {
        try
        {
            var result = await _authService.RegisterAsync(request);
            return Ok(ApiResponse<LoginResponse>.SuccessResponse(result, "Registration successful"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<LoginResponse>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login([FromBody] LoginRequest request)
    {
        try
        {
            var result = await _authService.LoginAsync(request);
            return Ok(ApiResponse<LoginResponse>.SuccessResponse(result, "Login successful"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<LoginResponse>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            var result = await _authService.RefreshTokenAsync(request.RefreshToken);
            return Ok(ApiResponse<LoginResponse>.SuccessResponse(result, "Token refreshed"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<LoginResponse>.ErrorResponse(ex.Message));
        }
    }

    [HttpPost("revoke")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> RevokeToken([FromBody] RefreshTokenRequest request)
    {
        try
        {
            await _authService.RevokeTokenAsync(request.RefreshToken);
            return Ok(ApiResponse<object>.SuccessResponse(null, "Token revoked"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    [HttpPut("change-password")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<object>>> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        try
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            await _authService.ChangePasswordAsync(userId, request);
            return Ok(ApiResponse<object>.SuccessResponse(null, "Password changed successfully"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    [HttpPut("lock-user/{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<ApiResponse<object>>> LockUser(int id)
    {
        try
        {
            await _authService.LockUserAsync(id);
            return Ok(ApiResponse<object>.SuccessResponse(null, "User locked successfully"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    [HttpPut("unlock-user/{id}")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<ApiResponse<object>>> UnlockUser(int id)
    {
        try
        {
            await _authService.UnlockUserAsync(id);
            return Ok(ApiResponse<object>.SuccessResponse(null, "User unlocked successfully"));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("users")]
    [Authorize(Roles = "ADMIN")]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserDto>>>> GetAllUsers()
    {
        try
        {
            var users = await _authService.GetAllUsersAsync();
            return Ok(ApiResponse<IEnumerable<UserDto>>.SuccessResponse(users));
        }
        catch (Exception ex)
        {
            return BadRequest(ApiResponse<IEnumerable<UserDto>>.ErrorResponse(ex.Message));
        }
    }

    [HttpGet("internal/users/{id:int}")]
    [AllowAnonymous]
    public async Task<ActionResult<InternalUserInfoDto>> GetInternalUserById(int id)
    {
        var user = await _authService.GetInternalUserInfoByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { error = $"User {id} not found" });
        }

        return Ok(user);
    }

    [HttpGet("internal/users")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<InternalUserInfoDto>>> GetInternalUsers([FromQuery] string ids)
    {
        if (string.IsNullOrWhiteSpace(ids))
        {
            return Ok(new List<InternalUserInfoDto>());
        }

        var userIds = ids.Split(',')
            .Select(s => int.TryParse(s.Trim(), out var id) ? (int?)id : null)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        if (userIds.Count == 0)
        {
            return Ok(new List<InternalUserInfoDto>());
        }

        var users = await _authService.GetInternalUserInfosByIdsAsync(userIds);
        return Ok(users);
    }

    [HttpGet("internal/users/by-roles")]
    [AllowAnonymous]
    public async Task<ActionResult<IEnumerable<InternalUserInfoDto>>> GetInternalUsersByRoles([FromQuery] string roles)
    {
        if (string.IsNullOrWhiteSpace(roles))
        {
            return Ok(new List<InternalUserInfoDto>());
        }

        var roleList = roles.Split(',')
            .Select(r => r.Trim())
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (roleList.Count == 0)
        {
            return Ok(new List<InternalUserInfoDto>());
        }

        var users = await _authService.GetInternalUserInfosByRolesAsync(roleList);
        return Ok(users);
    }
}

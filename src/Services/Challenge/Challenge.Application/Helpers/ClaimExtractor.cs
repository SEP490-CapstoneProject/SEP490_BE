using System.Security.Claims;

namespace Challenge.Application.Helpers;

/// <summary>
/// Helper for extracting claims from JWT tokens
/// </summary>
public static class ClaimExtractor
{
    /// <summary>
    /// Extract user ID from JWT claims
    /// </summary>
    public static int? GetUserId(ClaimsPrincipal user)
    {
        var claim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                 ?? user.FindFirst("sub")?.Value;
        return int.TryParse(claim, out var id) ? id : null;
    }

    /// <summary>
    /// Extract user role from JWT claims
    /// </summary>
    public static string? GetRole(ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Role)?.Value 
            ?? user.FindFirst("role")?.Value;
    }

    /// <summary>
    /// Check if user is admin or moderator
    /// </summary>
    public static bool IsAdminOrModerator(ClaimsPrincipal user)
    {
        var role = GetRole(user);
        return string.Equals(role, "ADMIN", StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, "MODERATOR", StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, "Moderator", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Check if user is admin
    /// </summary>
    public static bool IsAdmin(ClaimsPrincipal user)
    {
        var role = GetRole(user);
        return string.Equals(role, "ADMIN", StringComparison.OrdinalIgnoreCase)
            || string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase);
    }
}

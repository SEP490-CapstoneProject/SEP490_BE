using Challenge.Application.DTOs;

namespace Challenge.Application.Clients;

/// <summary>
/// Client for resolving actor/user information from other services
/// </summary>
public interface IActorResolverClient
{
    /// <summary>
    /// Get user information by user ID
    /// </summary>
    Task<UserInfoDto?> GetUserByIdAsync(int userId);

    /// <summary>
    /// Batch fetch user information
    /// </summary>
    Task<Dictionary<int, UserInfoDto>> GetUsersByIdsAsync(IEnumerable<int> userIds);

    /// <summary>
    /// Get employee information by user ID (for portfolio connection)
    /// </summary>
    Task<EmployeeInfoDto?> GetEmployeeByUserIdAsync(int userId);
}

/// <summary>
/// DTO for user information from UserProfile service
/// </summary>
public class UserInfoDto
{
    public int Id { get; set; }
    public string? FullName { get; set; }
    public string? Avatar { get; set; }
    public string? Email { get; set; }
    public int? EmployeeId { get; set; }
}

/// <summary>
/// DTO for employee information from UserProfile service
/// </summary>
public class EmployeeInfoDto
{
    public int Id { get; set; }
    public string? Name { get; set; }
    public string? Title { get; set; }
    public int? UserId { get; set; }
}

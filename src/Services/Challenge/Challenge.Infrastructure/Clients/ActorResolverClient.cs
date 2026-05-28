using System.Net.Http.Json;
using Challenge.Application.Clients;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Challenge.Infrastructure.Clients;

public class ActorResolverClient : IActorResolverClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ActorResolverClient> _logger;
    private readonly string _baseUrl;

    public ActorResolverClient(
        HttpClient httpClient,
        ILogger<ActorResolverClient> logger,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _logger = logger;
        _baseUrl = configuration["ServiceUrls:UserProfileService"]
            ?? throw new InvalidOperationException("Configuration key 'ServiceUrls:UserProfileService' is missing.");
    }

    public async Task<UserInfoDto?> GetUserByIdAsync(int userId)
    {
        try
        {
            var profile = await ResolveProfileAsync(userId);
            if (profile is null)
            {
                return null;
            }

            return new UserInfoDto
            {
                Id = userId,
                FullName = profile.Name,
                Avatar = profile.Avatar,
                Email = profile.Email
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user {UserId}", userId);
            return null;
        }
    }

    public async Task<Dictionary<int, UserInfoDto>> GetUsersByIdsAsync(IEnumerable<int> userIds)
    {
        var result = new Dictionary<int, UserInfoDto>();

        try
        {
            var ids = userIds.ToList();
            var users = await Task.WhenAll(ids.Select(GetUserByIdAsync));

            for (var i = 0; i < ids.Count; i++)
            {
                if (users[i] is not null)
                {
                    result[ids[i]] = users[i]!;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error batch fetching users");
        }

        return result;
    }

    public async Task<EmployeeInfoDto?> GetEmployeeByUserIdAsync(int userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/employee/by-user/{userId}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<EmployeeInfoDto>();
            }

            _logger.LogWarning("Failed to fetch employee for user {UserId}: {StatusCode}", userId, response.StatusCode);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching employee for user {UserId}", userId);
            return null;
        }
    }

    private async Task<UserProfileData?> ResolveProfileAsync(int userId)
    {
        return await TryGetEmployeeProfileAsync(userId)
            ?? await TryGetExpertProfileAsync(userId)
            ?? await TryGetCompanyProfileAsync(userId);
    }

    private async Task<UserProfileData?> TryGetEmployeeProfileAsync(int userId)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/employee/by-user/{userId}");
        if (response.IsSuccessStatusCode)
        {
            var employee = await response.Content.ReadFromJsonAsync<EmployeeProfileDto>();
            return employee is null
                ? null
                : new UserProfileData(employee.Name, employee.Avatar, employee.Email);
        }

        if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Failed to fetch employee profile for user {UserId}: {StatusCode}", userId, response.StatusCode);
        }

        return null;
    }

    private async Task<UserProfileData?> TryGetExpertProfileAsync(int userId)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/expert/by-user/{userId}");
        if (response.IsSuccessStatusCode)
        {
            var expert = await response.Content.ReadFromJsonAsync<ExpertProfileDto>();
            return expert is null
                ? null
                : new UserProfileData(expert.Name, expert.Avatar, expert.Email);
        }

        if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Failed to fetch expert profile for user {UserId}: {StatusCode}", userId, response.StatusCode);
        }

        return null;
    }

    private async Task<UserProfileData?> TryGetCompanyProfileAsync(int userId)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/api/company/by-user/{userId}");
        if (response.IsSuccessStatusCode)
        {
            var company = await response.Content.ReadFromJsonAsync<CompanyProfileDto>();
            return company is null
                ? null
                : new UserProfileData(company.CompanyName, company.Avatar, company.Email);
        }

        if (response.StatusCode != System.Net.HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Failed to fetch company profile for user {UserId}: {StatusCode}", userId, response.StatusCode);
        }

        return null;
    }

    private sealed record UserProfileData(string? Name, string? Avatar, string? Email);

    private sealed class EmployeeProfileDto
    {
        public string? Name { get; set; }
        public string? Avatar { get; set; }
        public string? Email { get; set; }
    }

    private sealed class ExpertProfileDto
    {
        public string? Name { get; set; }
        public string? Avatar { get; set; }
        public string? Email { get; set; }
    }

    private sealed class CompanyProfileDto
    {
        public string? CompanyName { get; set; }
        public string? Avatar { get; set; }
        public string? Email { get; set; }
    }
}

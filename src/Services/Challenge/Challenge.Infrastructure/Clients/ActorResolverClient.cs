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
        _baseUrl = configuration["Services:UserProfile:Url"]
            ?? configuration["UserProfile:Url"]
            ?? "https://userprofile-service:8080";
    }

    public async Task<UserInfoDto?> GetUserByIdAsync(int userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/api/users/{userId}");
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<UserInfoDto>();
            }

            _logger.LogWarning("Failed to fetch user {UserId}: {StatusCode}", userId, response.StatusCode);
            return null;
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
}

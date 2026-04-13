using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using UserProfile.Application.DTOs;
using UserProfile.Application.Interfaces;

namespace UserProfile.Application.Services;

public class AuthUserClient : IAuthUserClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AuthUserClient> _logger;

    public AuthUserClient(IHttpClientFactory httpClientFactory, ILogger<AuthUserClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<AuthUserInfoDto?> GetUserByIdAsync(int userId)
    {
        var client = _httpClientFactory.CreateClient("AuthService");
        var response = await client.GetAsync($"/api/auth/internal/users/{userId}");
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<AuthUserInfoDto>();
    }

    public async Task<Dictionary<int, AuthUserInfoDto>> GetUsersByIdsAsync(IEnumerable<int> userIds)
    {
        var idList = userIds.Distinct().ToList();
        if (idList.Count == 0)
        {
            return new Dictionary<int, AuthUserInfoDto>();
        }

        var client = _httpClientFactory.CreateClient("AuthService");
        var idsParam = string.Join(",", idList);
        var response = await client.GetAsync($"/api/auth/internal/users?ids={Uri.EscapeDataString(idsParam)}");
        response.EnsureSuccessStatusCode();

        var users = await response.Content.ReadFromJsonAsync<List<AuthUserInfoDto>>() ?? new List<AuthUserInfoDto>();
        return users.ToDictionary(u => u.Id);
    }
}

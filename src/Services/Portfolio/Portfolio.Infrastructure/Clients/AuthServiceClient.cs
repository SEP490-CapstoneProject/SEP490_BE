using System.Net.Http.Json;
using Portfolio.Application.Interfaces;

namespace Portfolio.Infrastructure.Clients;

public class AuthServiceClient : IAuthServiceClient
{
    private readonly HttpClient _http;

    public AuthServiceClient(HttpClient http)
    {
        _http = http;
    }

    public async Task<Dictionary<int, AuthInternalUserDto>> GetUsersByIdsAsync(IEnumerable<int> userIds)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<int, AuthInternalUserDto>();
        }

        var idsParam = string.Join(",", ids);
        var response = await _http.GetAsync($"/api/auth/internal/users?ids={Uri.EscapeDataString(idsParam)}");
        if (!response.IsSuccessStatusCode)
        {
            return new Dictionary<int, AuthInternalUserDto>();
        }

        var users = await response.Content.ReadFromJsonAsync<List<AuthInternalUserDto>>() ?? new List<AuthInternalUserDto>();
        return users.ToDictionary(x => x.Id);
    }
}

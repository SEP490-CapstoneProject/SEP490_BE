using System.Text.Json;
using Connection.Application.DTOs;
using Connection.Application.Interfaces;

namespace Connection.API.Services;

public sealed class UserProfileResolver : IUserProfileResolver
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<UserProfileResolver> _logger;

    public UserProfileResolver(
        IHttpClientFactory httpClientFactory,
        ILogger<UserProfileResolver> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<NotificationActorDto?> ResolveAsync(int userId, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = _httpClientFactory.CreateClient("UserProfile");

            var companyRes = await client.GetAsync($"/api/company/by-user/{userId}", cancellationToken);
            if (companyRes.IsSuccessStatusCode)
            {
                var json = await companyRes.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(json);

                return new NotificationActorDto
                {
                    Id = userId,
                    Name = doc.RootElement.TryGetProperty("companyName", out var companyName)
                        ? companyName.GetString() ?? userId.ToString()
                        : userId.ToString(),
                    Avatar = doc.RootElement.TryGetProperty("avatar", out var avatar)
                        ? avatar.GetString() ?? string.Empty
                        : string.Empty,
                    Role = "COMPANY"
                };
            }

            var employeeRes = await client.GetAsync($"/api/employee/by-user/{userId}", cancellationToken);
            if (employeeRes.IsSuccessStatusCode)
            {
                var json = await employeeRes.Content.ReadAsStringAsync(cancellationToken);
                using var doc = JsonDocument.Parse(json);

                return new NotificationActorDto
                {
                    Id = userId,
                    Name = doc.RootElement.TryGetProperty("name", out var name)
                        ? name.GetString() ?? userId.ToString()
                        : userId.ToString(),
                    Avatar = doc.RootElement.TryGetProperty("avatar", out var avatar)
                        ? avatar.GetString() ?? string.Empty
                        : string.Empty,
                    Role = "USER"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Unable to resolve user profile for user {UserId}", userId);
        }

        return null;
    }
}

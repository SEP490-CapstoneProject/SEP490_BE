using System.Text.Json;
using Microsoft.Extensions.Logging;
using Notification.Application.DTOs;
using Notification.Application.Interfaces;

namespace Notification.Infrastructure.Clients;

public class RecipientResolverClient : IRecipientResolverClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly ILogger<RecipientResolverClient> _logger;

    public RecipientResolverClient(HttpClient httpClient, ILogger<RecipientResolverClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<string>> GetActiveUserIdsByRolesAsync(IEnumerable<string> roles, CancellationToken cancellationToken = default)
    {
        var normalizedRoles = roles
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Select(r => r.Trim().ToUpperInvariant())
            .Distinct()
            .ToList();

        if (normalizedRoles.Count == 0)
        {
            return Array.Empty<string>();
        }

        var rolesQuery = string.Join(",", normalizedRoles);

        try
        {
            var response = await _httpClient.GetAsync($"/api/auth/internal/users/by-roles?roles={Uri.EscapeDataString(rolesQuery)}", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Recipient resolve failed. Roles={Roles}, StatusCode={StatusCode}", rolesQuery, response.StatusCode);
                return Array.Empty<string>();
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var users = JsonSerializer.Deserialize<List<InternalUserInfoDto>>(json, JsonOptions) ?? new List<InternalUserInfoDto>();

            return users
                .Where(u => u.Id > 0 && string.Equals(u.Status, "Active", StringComparison.OrdinalIgnoreCase))
                .Select(u => u.Id.ToString())
                .Distinct()
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Recipient resolve exception. Roles={Roles}", rolesQuery);
            return Array.Empty<string>();
        }
    }
}

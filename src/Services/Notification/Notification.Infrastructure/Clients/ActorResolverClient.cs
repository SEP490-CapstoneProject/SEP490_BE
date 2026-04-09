using System.Text.Json;
using System.Net;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Notification.Application.DTOs;
using Notification.Application.Interfaces;

namespace Notification.Infrastructure.Clients;

public class ActorResolverClient : IActorResolverClient
{
    private readonly HttpClient _httpClient;
    private readonly IMemoryCache _cache;
    private readonly ILogger<ActorResolverClient> _logger;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public ActorResolverClient(HttpClient httpClient, IMemoryCache cache, ILogger<ActorResolverClient> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _logger = logger;
    }

    public async Task<ActorDto?> ResolveActorAsync(string actorId, string actorType)
    {
        if (string.IsNullOrEmpty(actorId) || actorType == "SYSTEM") return null;

        var cacheKey = $"actor:{actorId}";
        if (_cache.TryGetValue(cacheKey, out ActorDto? cached))
            return cached;

        try
        {
            ActorDto? actor = null;
            if (string.Equals(actorType, "COMPANY", StringComparison.OrdinalIgnoreCase))
            {
                actor = await TryResolveCompanyAsync(actorId) ?? await TryResolveEmployeeAsync(actorId);
            }
            else
            {
                actor = await TryResolveEmployeeAsync(actorId) ?? await TryResolveCompanyAsync(actorId);
            }

            if (actor is null) return null;

            _cache.Set(cacheKey, actor, TimeSpan.FromMinutes(2));
            return actor;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve actor {ActorId}", actorId);
            return null;
        }
    }

    private async Task<ActorDto?> TryResolveEmployeeAsync(string actorId)
    {
        var response = await _httpClient.GetAsync($"/api/employee/by-user/{actorId}");
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        var profile = JsonSerializer.Deserialize<EmployeeProfileResponse>(json, _jsonOptions);
        if (profile is null) return null;

        return new ActorDto
        {
            Id = int.TryParse(actorId, out var parsedActorId) ? parsedActorId : 0,
            Name = string.IsNullOrWhiteSpace(profile.Name) ? actorId : profile.Name,
            Avatar = profile.Avatar ?? string.Empty,
            Role = "USER"
        };
    }

    private async Task<ActorDto?> TryResolveCompanyAsync(string actorId)
    {
        var response = await _httpClient.GetAsync($"/api/company/by-user/{actorId}");
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadAsStringAsync();
        var profile = JsonSerializer.Deserialize<CompanyProfileResponse>(json, _jsonOptions);
        if (profile is null) return null;

        return new ActorDto
        {
            Id = int.TryParse(actorId, out var parsedActorId) ? parsedActorId : 0,
            Name = string.IsNullOrWhiteSpace(profile.CompanyName) ? actorId : profile.CompanyName,
            Avatar = profile.Avatar ?? string.Empty,
            Role = "COMPANY"
        };
    }

    private class EmployeeProfileResponse
    {
        public string? Name { get; set; }
        public string? Avatar { get; set; }
    }

    private class CompanyProfileResponse
    {
        public string? CompanyName { get; set; }
        public string? Avatar { get; set; }
    }
}

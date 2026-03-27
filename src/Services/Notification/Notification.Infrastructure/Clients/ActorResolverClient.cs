using System.Text.Json;
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
            var response = await _httpClient.GetAsync($"/api/users/{actorId}");
            if (!response.IsSuccessStatusCode)
                return null;

            var json = await response.Content.ReadAsStringAsync();
            var profile = JsonSerializer.Deserialize<UserProfileResponse>(json, _jsonOptions);
            if (profile is null) return null;

            var actor = new ActorDto
            {
                Id = actorId,
                Name = profile.FullName ?? profile.Username ?? actorId,
                AvatarUrl = profile.AvatarUrl ?? profile.ProfilePicture
            };

            _cache.Set(cacheKey, actor, TimeSpan.FromMinutes(2));
            return actor;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve actor {ActorId}", actorId);
            return null;
        }
    }

    private class UserProfileResponse
    {
        public string? FullName { get; set; }
        public string? Username { get; set; }
        public string? AvatarUrl { get; set; }
        public string? ProfilePicture { get; set; }
    }
}

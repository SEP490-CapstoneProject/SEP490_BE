using System.Net.Http.Json;
using Application.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.Infrastructure.Clients;

public class SubscriptionClient : ISubscriptionClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SubscriptionClient> _logger;

    public SubscriptionClient(HttpClient httpClient, ILogger<SubscriptionClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<EntitlementsDto?> GetEntitlementsAsync(int userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/subscriptions/entitlements/{userId}");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get entitlements for user {UserId}: {StatusCode}", 
                    userId, response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<EntitlementsDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting entitlements for user {UserId}", userId);
            return null;
        }
    }
}

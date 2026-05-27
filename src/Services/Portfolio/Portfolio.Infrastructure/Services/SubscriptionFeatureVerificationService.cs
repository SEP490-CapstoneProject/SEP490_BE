using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Subscription.Application.DTOs;
using Subscription.Application.Interfaces;

namespace Portfolio.Infrastructure.Services;

public class SubscriptionFeatureVerificationService : IFeatureVerificationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SubscriptionFeatureVerificationService> _logger;

    public SubscriptionFeatureVerificationService(
        HttpClient httpClient,
        ILogger<SubscriptionFeatureVerificationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> HasFeatureAccessAsync(int userId, string featureKey)
    {
        var value = await GetFeatureValueAsync(userId, featureKey);
        if (value == null)
        {
            return false;
        }

        return !bool.TryParse(value, out var boolValue) || boolValue;
    }

    public async Task<string?> GetFeatureValueAsync(int userId, string featureKey)
    {
        var entitlements = await GetUserEntitlementsAsync(userId);
        return entitlements.TryGetValue(featureKey, out var value) ? value : null;
    }

    public async Task<bool> CanPerformActionAsync(int userId, string actionKey, int currentCount = 0)
    {
        var value = await GetFeatureValueAsync(userId, actionKey);
        if (value == null)
        {
            return false;
        }

        if (value == "-1")
        {
            return true;
        }

        if (!int.TryParse(value, out var limit))
        {
            _logger.LogWarning("Invalid numeric value '{Value}' for feature {FeatureKey}", value, actionKey);
            return false;
        }

        return currentCount < limit;
    }

    public async Task<FeatureEntitlementDto?> GetFeatureEntitlementAsync(int userId, string featureKey)
    {
        var entitlements = await GetEntitlementsAsync(userId);
        if (!entitlements.Features.TryGetValue(featureKey, out var value))
        {
            return null;
        }

        return new FeatureEntitlementDto
        {
            FeatureKey = featureKey,
            FeatureName = featureKey,
            Value = NormalizeValue(value),
            Type = ResolveType(value),
            IsActive = true
        };
    }

    public async Task<Dictionary<string, string>> GetUserEntitlementsAsync(int userId)
    {
        var entitlements = await GetEntitlementsAsync(userId);
        return ConvertToStringDictionary(entitlements.Features);
    }

    private async Task<EntitlementsDto> GetEntitlementsAsync(int userId)
    {
        var response = await _httpClient.GetAsync($"/api/subscriptions/entitlements/{userId}");
        response.EnsureSuccessStatusCode();

        var entitlements = await response.Content.ReadFromJsonAsync<EntitlementsDto>();
        if (entitlements == null)
        {
            throw new InvalidOperationException($"Subscription entitlements not found for user {userId}.");
        }

        return entitlements;
    }

    private static Dictionary<string, string> ConvertToStringDictionary(Dictionary<string, object> features)
    {
        var result = new Dictionary<string, string>();
        foreach (var (key, value) in features)
        {
            result[key] = NormalizeValue(value);
        }

        return result;
    }

    private static string NormalizeValue(object value)
    {
        return value switch
        {
            null => string.Empty,
            bool boolValue => boolValue.ToString().ToLowerInvariant(),
            int intValue => intValue.ToString(),
            long longValue => longValue.ToString(),
            decimal decimalValue => decimalValue.ToString(),
            JsonElement jsonElement => JsonElementToString(jsonElement),
            _ => value.ToString() ?? string.Empty
        };
    }

    private static string ResolveType(object value)
    {
        return value switch
        {
            bool => "Boolean",
            int => "Number",
            long => "Number",
            decimal => "Number",
            JsonElement jsonElement => ResolveJsonType(jsonElement),
            _ => "Text"
        };
    }

    private static string ResolveJsonType(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.True => "Boolean",
            JsonValueKind.False => "Boolean",
            JsonValueKind.Number => "Number",
            _ => "Text"
        };
    }

    private static string JsonElementToString(JsonElement element)
    {
        return element.ValueKind switch
        {
            JsonValueKind.String => element.GetString() ?? string.Empty,
            JsonValueKind.True => "true",
            JsonValueKind.False => "false",
            JsonValueKind.Number when element.TryGetInt64(out var intValue) => intValue.ToString(),
            JsonValueKind.Number when element.TryGetDecimal(out var decimalValue) => decimalValue.ToString(),
            _ => element.ToString()
        };
    }
}

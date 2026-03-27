using System.Text.Json;
using Microsoft.Extensions.Logging;
using Payment.Application.Interfaces;

namespace Payment.Infrastructure.Services;

public class HttpPlanPriceProvider : IPlanPriceProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<HttpPlanPriceProvider> _logger;

    public HttpPlanPriceProvider(HttpClient httpClient, ILogger<HttpPlanPriceProvider> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<(decimal price, string planName)?> GetPlanPriceAsync(int planId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/plans/{planId}");
            
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to get plan {PlanId}, status: {StatusCode}", 
                    planId, response.StatusCode);
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var json = JsonDocument.Parse(content);
            var root = json.RootElement;

            var price = root.GetProperty("price").GetDecimal();
            var name = root.GetProperty("name").GetString() ?? $"Plan {planId}";

            return (price, name);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting price for plan {PlanId}", planId);
            return null;
        }
    }
}

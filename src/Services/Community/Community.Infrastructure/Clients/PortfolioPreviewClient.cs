using Community.Application.Clients;
using Community.Application.DTOs;
using System.Net.Http.Json;
using System.Text.Json;

namespace Community.Infrastructure.Clients;

public class PortfolioPreviewClient : IPortfolioPreviewClient
{
    private readonly HttpClient _httpClient;

    public PortfolioPreviewClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<PortfolioPreviewDto?> GetPreviewAsync(int portfolioId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/portfolio/{portfolioId}/preview");
            if (!response.IsSuccessStatusCode) return null;

            return await response.Content.ReadFromJsonAsync<PortfolioPreviewDto>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        }
        catch
        {
            return null;
        }
    }
}

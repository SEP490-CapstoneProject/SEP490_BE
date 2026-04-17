using System.Net.Http.Json;
using Company.Application.Clients;
using Company.Application.DTOs;
using Microsoft.Extensions.Logging;
using RecruitmentPlatform.AI.Models;

namespace Company.Infrastructure.Clients;

public sealed class PortfolioMatchingClient : IPortfolioMatchingClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PortfolioMatchingClient> _logger;

    public PortfolioMatchingClient(HttpClient httpClient, ILogger<PortfolioMatchingClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<MatchingCandidate>> GetPortfolioCandidatesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/portfolio/internal/matching-candidates?limit=150", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Portfolio matching candidates request failed with status {Status}", response.StatusCode);
                return Array.Empty<MatchingCandidate>();
            }

            var feed = await response.Content.ReadFromJsonAsync<MatchingCandidateFeed>(cancellationToken: cancellationToken);
            return feed?.Items ?? new List<MatchingCandidate>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Portfolio matching candidates request failed");
            return Array.Empty<MatchingCandidate>();
        }
    }
}

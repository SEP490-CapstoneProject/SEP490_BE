using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;
using RecruitmentPlatform.AI.Models;

namespace Portfolio.Infrastructure.Clients;

public sealed class CompanyMatchingClient : ICompanyMatchingClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CompanyMatchingClient> _logger;

    public CompanyMatchingClient(HttpClient httpClient, ILogger<CompanyMatchingClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<IReadOnlyList<MatchingCandidate>> GetJobCandidatesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync("/api/company-posts/internal/matching-candidates?limit=150", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Company matching candidates request failed with status {Status}", response.StatusCode);
                return Array.Empty<MatchingCandidate>();
            }

            var feed = await response.Content.ReadFromJsonAsync<MatchingCandidateFeed>(cancellationToken: cancellationToken);
            return feed?.Items ?? new List<MatchingCandidate>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Company matching candidates request failed");
            return Array.Empty<MatchingCandidate>();
        }
    }
}

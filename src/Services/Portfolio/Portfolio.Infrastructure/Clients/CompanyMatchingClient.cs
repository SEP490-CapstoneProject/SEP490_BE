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
        const string endpoint = "/api/company-posts/internal/matching-candidates?limit=500";
        try
        {
            var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Company matching candidates request failed. Endpoint: {Endpoint}, Status: {Status}", endpoint, response.StatusCode);
                return Array.Empty<MatchingCandidate>();
            }

            var feed = await response.Content.ReadFromJsonAsync<MatchingCandidateFeed>(cancellationToken: cancellationToken);
            return feed?.Items ?? new List<MatchingCandidate>();
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Company matching candidates request timed out. Endpoint: {Endpoint}", endpoint);
            return Array.Empty<MatchingCandidate>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Company matching candidates request failed. Endpoint: {Endpoint}", endpoint);
            return Array.Empty<MatchingCandidate>();
        }
    }

    public async Task<IReadOnlyList<CompanyPostDetailForMatchDto>> GetPostsByIdsAsync(IEnumerable<int> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0) return Array.Empty<CompanyPostDetailForMatchDto>();

        var idsParam = string.Join(",", idList);
        var endpoint = $"/api/company-posts/batch?ids={idsParam}";
        try
        {
            var response = await _httpClient.GetAsync(endpoint, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Company posts batch request failed. Endpoint: {Endpoint}, Status: {Status}", endpoint, response.StatusCode);
                return Array.Empty<CompanyPostDetailForMatchDto>();
            }

            var result = await response.Content.ReadFromJsonAsync<List<CompanyPostDetailForMatchDto>>(cancellationToken: cancellationToken);
            return result ?? new List<CompanyPostDetailForMatchDto>();
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Company posts batch request timed out. Endpoint: {Endpoint}", endpoint);
            return Array.Empty<CompanyPostDetailForMatchDto>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Company posts batch request failed. Endpoint: {Endpoint}", endpoint);
            return Array.Empty<CompanyPostDetailForMatchDto>();
        }
    }
}

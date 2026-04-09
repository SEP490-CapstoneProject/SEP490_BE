using Company.Application.Clients;
using Company.Application.DTOs;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;

namespace Company.Infrastructure.Clients;

public class UserProfileCompanyClient : ICompanyProfileClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UserProfileCompanyClient> _logger;

    public UserProfileCompanyClient(HttpClient httpClient, ILogger<UserProfileCompanyClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CompanyProfileDto?> GetCompanyAsync(int companyId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/company/{companyId}");
            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("UserProfile company lookup failed: {Status} for {CompanyId}", response.StatusCode, companyId);
                return null;
            }

            var dto = await response.Content.ReadFromJsonAsync<CompanyProfileDto>();
            return dto;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Exception calling UserProfile for company {CompanyId}", companyId);
            return null;
        }
    }

    public async Task<IReadOnlyList<CompanyProfileDto>> GetCompaniesAsync(IEnumerable<int> companyIds)
    {
        var ids = companyIds.Distinct().ToList();
        if (ids.Count == 0) return Array.Empty<CompanyProfileDto>();

        try
        {
            var query = string.Join(",", ids);
            var response = await _httpClient.GetAsync($"/api/company/batch?ids={query}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("UserProfile batch lookup failed: {Status}", response.StatusCode);
                return Array.Empty<CompanyProfileDto>();
            }

            var result = await response.Content.ReadFromJsonAsync<List<CompanyProfileDto>>();
            return result ?? new List<CompanyProfileDto>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Exception calling UserProfile batch");
            return Array.Empty<CompanyProfileDto>();
        }
    }
}

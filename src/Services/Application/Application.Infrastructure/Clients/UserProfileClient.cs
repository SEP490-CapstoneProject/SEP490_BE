using System.Net.Http.Json;
using System.Text.Json;
using Application.Application.DTOs;
using Application.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Application.Infrastructure.Clients;

public class UserProfileClient : IUserProfileClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UserProfileClient> _logger;

    public UserProfileClient(HttpClient httpClient, ILogger<UserProfileClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<EmployeeDto?> GetEmployeeByIdAsync(int employeeId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/employees/{employeeId}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<EmployeeDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get employee {EmployeeId}", employeeId);
            throw;
        }
    }

    public async Task<CompanyExternalDto?> GetCompanyByIdAsync(int companyId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/companies/{companyId}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<CompanyExternalDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get company {CompanyId}", companyId);
            throw;
        }
    }

    public async Task<CompanyPostDto?> GetCompanyPostByIdAsync(int companyPostId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/posts/{companyPostId}");
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<CompanyPostDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get post {PostId}", companyPostId);
            throw;
        }
    }

    public async Task<bool> ValidatePortfolioOwnershipAsync(int employeeId, int portfolioId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/portfolios/{portfolioId}/owner/{employeeId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate portfolio {PortfolioId} for employee {EmployeeId}", portfolioId, employeeId);
            return false;
        }
    }

    public async Task<Dictionary<int, EmployeeDto>> GetEmployeesByIdsAsync(List<int> employeeIds)
    {
        if (!employeeIds.Any()) return new Dictionary<int, EmployeeDto>();

        try
        {
            var ids = string.Join(",", employeeIds);
            var response = await _httpClient.GetAsync($"/api/employees?ids={ids}");
            response.EnsureSuccessStatusCode();
            var employees = await response.Content.ReadFromJsonAsync<List<EmployeeDto>>() ?? new List<EmployeeDto>();
            return employees.ToDictionary(e => e.EmployeeId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get employees by IDs");
            return new Dictionary<int, EmployeeDto>();
        }
    }

    public async Task<Dictionary<int, CompanyExternalDto>> GetCompaniesByIdsAsync(List<int> companyIds)
    {
        if (!companyIds.Any()) return new Dictionary<int, CompanyExternalDto>();

        try
        {
            var ids = string.Join(",", companyIds);
            var response = await _httpClient.GetAsync($"/api/companies?ids={ids}");
            response.EnsureSuccessStatusCode();
            var companies = await response.Content.ReadFromJsonAsync<List<CompanyExternalDto>>() ?? new List<CompanyExternalDto>();
            return companies.ToDictionary(c => c.CompanyId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get companies by IDs");
            return new Dictionary<int, CompanyExternalDto>();
        }
    }

    public async Task<Dictionary<int, CompanyPostDto>> GetPostsByIdsAsync(List<int> postIds)
    {
        if (!postIds.Any()) return new Dictionary<int, CompanyPostDto>();

        try
        {
            var ids = string.Join(",", postIds);
            var response = await _httpClient.GetAsync($"/api/posts?ids={ids}");
            response.EnsureSuccessStatusCode();
            var posts = await response.Content.ReadFromJsonAsync<List<CompanyPostDto>>() ?? new List<CompanyPostDto>();
            return posts.ToDictionary(p => p.PostId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get posts by IDs");
            return new Dictionary<int, CompanyPostDto>();
        }
    }
}

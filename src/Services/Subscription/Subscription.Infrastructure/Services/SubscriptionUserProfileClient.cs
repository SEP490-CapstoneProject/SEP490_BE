using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using Subscription.Application.Interfaces;

namespace Subscription.Infrastructure.Services;

public class SubscriptionUserProfileClient : ISubscriptionUserProfileClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SubscriptionUserProfileClient> _logger;

    public SubscriptionUserProfileClient(
        HttpClient httpClient,
        ILogger<SubscriptionUserProfileClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Dictionary<int, SubscriptionUserProfileDto>> GetUsersBatchAsync(IEnumerable<int> userIds)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new Dictionary<int, SubscriptionUserProfileDto>();
        }

        var query = string.Join(",", ids);
        var employeesTask = FetchEmployeesAsync(query);
        var companiesTask = FetchCompaniesAsync(query);

        await Task.WhenAll(employeesTask, companiesTask);

        var result = new Dictionary<int, SubscriptionUserProfileDto>();

        foreach (var employee in employeesTask.Result)
        {
            result[employee.UserId] = new SubscriptionUserProfileDto
            {
                UserId = employee.UserId,
                Name = employee.Name,
                Avatar = employee.Avatar ?? string.Empty
            };
        }

        foreach (var company in companiesTask.Result)
        {
            if (!result.ContainsKey(company.UserId))
            {
                result[company.UserId] = new SubscriptionUserProfileDto
                {
                    UserId = company.UserId,
                    Name = company.CompanyName,
                    Avatar = company.Avatar ?? string.Empty
                };
            }
        }

        foreach (var id in ids)
        {
            if (!result.ContainsKey(id))
            {
                result[id] = new SubscriptionUserProfileDto
                {
                    UserId = id,
                    Name = "Unknown",
                    Avatar = string.Empty
                };
            }
        }

        return result;
    }

    private async Task<List<EmployeeProfileResponse>> FetchEmployeesAsync(string userIds)
    {
        var response = await _httpClient.GetAsync($"/api/employee/batch?userIds={userIds}");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Employee batch profile lookup failed with status {StatusCode}", response.StatusCode);
            return new List<EmployeeProfileResponse>();
        }

        return await response.Content.ReadFromJsonAsync<List<EmployeeProfileResponse>>() ?? new List<EmployeeProfileResponse>();
    }

    private async Task<List<CompanyProfileResponse>> FetchCompaniesAsync(string userIds)
    {
        var response = await _httpClient.GetAsync($"/api/company/batch?userIds={userIds}");
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Company batch profile lookup failed with status {StatusCode}", response.StatusCode);
            return new List<CompanyProfileResponse>();
        }

        return await response.Content.ReadFromJsonAsync<List<CompanyProfileResponse>>() ?? new List<CompanyProfileResponse>();
    }

    private sealed class EmployeeProfileResponse
    {
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Avatar { get; set; }
    }

    private sealed class CompanyProfileResponse
    {
        public int UserId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string? Avatar { get; set; }
    }
}

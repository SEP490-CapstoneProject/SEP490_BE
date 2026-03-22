using Community.Application.Clients;
using Community.Application.DTOs;
using System.Net.Http.Json;

namespace Community.Infrastructure.Clients;

public class UserInfoClient : IUserInfoClient
{
    private readonly HttpClient _httpClient;

    public UserInfoClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<Dictionary<int, AuthorDto>> GetAuthorsBatchAsync(IEnumerable<int> userIds)
    {
        var ids = userIds.Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<int, AuthorDto>();

        var query = string.Join(",", ids);

        // Fetch employees and companies in parallel
        var employeeTask = FetchEmployeesAsync(query);
        var companyTask = FetchCompaniesAsync(query);

        await Task.WhenAll(employeeTask, companyTask);

        var result = new Dictionary<int, AuthorDto>();

        // Map employees first (role = USER)
        foreach (var emp in employeeTask.Result)
        {
            result[emp.UserId] = new AuthorDto
            {
                Id = emp.UserId,
                Name = emp.Name,
                Avatar = emp.Avatar ?? string.Empty,
                Role = "USER"
            };
        }

        // Map companies for remaining userIds (role = COMPANY)
        foreach (var company in companyTask.Result)
        {
            if (!result.ContainsKey(company.UserId))
            {
                result[company.UserId] = new AuthorDto
                {
                    Id = company.UserId,
                    Name = company.CompanyName,
                    Avatar = company.Avatar ?? string.Empty,
                    Role = "COMPANY"
                };
            }
        }

        // Fallback for any userId with no profile
        foreach (var id in ids)
        {
            if (!result.ContainsKey(id))
            {
                result[id] = new AuthorDto { Id = id, Name = "Unknown", Avatar = string.Empty, Role = "USER" };
            }
        }

        return result;
    }

    private async Task<List<EmployeeProfileResponse>> FetchEmployeesAsync(string userIds)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/employee/batch?userIds={userIds}");
            if (!response.IsSuccessStatusCode) return new List<EmployeeProfileResponse>();
            return await response.Content.ReadFromJsonAsync<List<EmployeeProfileResponse>>()
                   ?? new List<EmployeeProfileResponse>();
        }
        catch { return new List<EmployeeProfileResponse>(); }
    }

    private async Task<List<CompanyProfileResponse>> FetchCompaniesAsync(string userIds)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/company/batch?userIds={userIds}");
            if (!response.IsSuccessStatusCode) return new List<CompanyProfileResponse>();
            return await response.Content.ReadFromJsonAsync<List<CompanyProfileResponse>>()
                   ?? new List<CompanyProfileResponse>();
        }
        catch { return new List<CompanyProfileResponse>(); }
    }

    private class EmployeeProfileResponse
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Avatar { get; set; }
    }

    private class CompanyProfileResponse
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string? Avatar { get; set; }
    }
}

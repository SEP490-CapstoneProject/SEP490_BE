using Auth.Application.Interfaces;
using System.Net.Http.Json;

namespace Auth.Infrastructure.Clients;

public class UserProfileClient : IUserProfileClient
{
    private readonly HttpClient _httpClient;

    public UserProfileClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<int?> GetEmployeeIdByUserIdAsync(int userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/employee/by-user/{userId}");
            if (!response.IsSuccessStatusCode) return null;

            var employee = await response.Content.ReadFromJsonAsync<EmployeeResponse>();
            return employee?.Id;
        }
        catch
        {
            return null;
        }
    }

    public async Task<int?> GetCompanyIdByUserIdAsync(int userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/company/by-user/{userId}");
            if (!response.IsSuccessStatusCode) return null;

            var company = await response.Content.ReadFromJsonAsync<CompanyResponse>();
            return company?.Id;
        }
        catch
        {
            return null;
        }
    }

    private class EmployeeResponse
    {
        public int Id { get; set; }
    }

    private class CompanyResponse
    {
        public int Id { get; set; }
    }
}

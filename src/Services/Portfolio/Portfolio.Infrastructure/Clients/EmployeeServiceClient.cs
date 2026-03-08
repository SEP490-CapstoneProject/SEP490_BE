using Microsoft.Extensions.Logging;
using Portfolio.Application.Interfaces;

namespace Portfolio.Infrastructure.Clients;

public class EmployeeServiceClient : IEmployeeServiceClient
{
    private readonly HttpClient _http;
    private readonly ILogger<EmployeeServiceClient> _logger;

    public EmployeeServiceClient(HttpClient http, ILogger<EmployeeServiceClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<bool> ValidateEmployeeAsync(int employeeId)
    {
        try
        {
            var response = await _http.GetAsync($"/api/employee/{employeeId}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not validate employee {EmployeeId}", employeeId);
            return false;
        }
    }
}

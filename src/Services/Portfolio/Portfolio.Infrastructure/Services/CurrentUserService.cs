using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Portfolio.Application.Interfaces;

namespace Portfolio.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _http;

    public CurrentUserService(IHttpContextAccessor http) => _http = http;

    private ClaimsPrincipal? User => _http.HttpContext?.User;

    public bool HasCompany => int.TryParse(
        User?.FindFirst("companyId")?.Value ?? User?.FindFirst("CompanyId")?.Value, out _);

    public int CompanyId
    {
        get
        {
            var raw = User?.FindFirst("companyId")?.Value ?? User?.FindFirst("CompanyId")?.Value;
            return int.TryParse(raw, out var id) ? id : 0;
        }
    }

    public int EmployeeId
    {
        get
        {
            var raw = User?.FindFirst("employeeId")?.Value ?? User?.FindFirst("EmployeeId")?.Value;
            return int.TryParse(raw, out var id) ? id : 0;
        }
    }

    public bool IsAdmin => User?.IsInRole("admin") ?? false;
}

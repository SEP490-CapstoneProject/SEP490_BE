using System.Security.Claims;
using Application.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace Application.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _http;

    public CurrentUserService(IHttpContextAccessor http) => _http = http;

    private ClaimsPrincipal? User => _http.HttpContext?.User;

    public int GetEmployeeId()
    {
        var claim = User?.FindFirst("employeeId")?.Value ?? User?.FindFirst("EmployeeId")?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }

    public int GetCompanyId()
    {
        var claim = User?.FindFirst("companyId")?.Value ?? User?.FindFirst("CompanyId")?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }

    public int GetUserId()
    {
        var claim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                 ?? User?.FindFirst("sub")?.Value
                 ?? User?.FindFirst("userId")?.Value;
        return int.TryParse(claim, out var id) ? id : 0;
    }

    public bool IsEmployee() => User?.IsInRole("USER") ?? false;

    public bool IsCompany() => User?.IsInRole("RECRUITER") ?? false;
}

namespace Portfolio.Application.Interfaces;

public interface ICurrentUserService
{
    bool HasCompany { get; }    // true if JWT has valid companyId claim
    int CompanyId { get; }      // only meaningful when HasCompany=true
    int EmployeeId { get; }
    bool IsAdmin { get; }
}

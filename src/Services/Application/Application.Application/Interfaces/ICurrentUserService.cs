namespace Application.Application.Interfaces;

public interface ICurrentUserService
{
    int GetEmployeeId();
    int GetCompanyId();
    int GetUserId();
    bool IsEmployee();
    bool IsCompany();
}

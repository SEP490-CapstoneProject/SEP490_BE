namespace Portfolio.Application.Interfaces;

public interface IEmployeeServiceClient
{
    Task<bool> ValidateEmployeeAsync(int employeeId);
}

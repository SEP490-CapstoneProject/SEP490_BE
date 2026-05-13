namespace Portfolio.Application.Interfaces;

public interface IEmployeeServiceClient
{
    Task<bool> ValidateEmployeeAsync(int employeeId);
    Task<EmployeeProfileDto?> GetEmployeeByIdAsync(int employeeId);
}

public class EmployeeProfileDto
{
    public int EmployeeId { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
}

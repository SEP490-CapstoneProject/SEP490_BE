namespace Auth.Application.Interfaces;

public interface IUserProfileClient
{
    Task<int?> GetEmployeeIdByUserIdAsync(int userId);
    Task<int?> GetCompanyIdByUserIdAsync(int userId);
}

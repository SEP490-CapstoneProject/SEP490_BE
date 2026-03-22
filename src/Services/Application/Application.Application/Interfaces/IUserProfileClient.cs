using Application.Application.DTOs;

namespace Application.Application.Interfaces;

public interface IUserProfileClient
{
    Task<EmployeeDto?> GetEmployeeByIdAsync(int employeeId);
    Task<CompanyExternalDto?> GetCompanyByIdAsync(int companyId);
    Task<CompanyPostDto?> GetCompanyPostByIdAsync(int companyPostId);
    Task<bool> ValidatePortfolioOwnershipAsync(int employeeId, int portfolioId);
    
    // Batch methods
    Task<Dictionary<int, EmployeeDto>> GetEmployeesByIdsAsync(List<int> employeeIds);
    Task<Dictionary<int, CompanyExternalDto>> GetCompaniesByIdsAsync(List<int> companyIds);
    Task<Dictionary<int, CompanyPostDto>> GetPostsByIdsAsync(List<int> postIds);
}

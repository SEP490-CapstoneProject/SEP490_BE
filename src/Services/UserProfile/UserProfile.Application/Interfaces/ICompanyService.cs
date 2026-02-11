using Microsoft.AspNetCore.Http;
using UserProfile.Application.DTOs;

namespace UserProfile.Application.Interfaces;

public interface ICompanyService
{
    Task<CompanyDto?> GetByIdAsync(int id);
    Task<CompanyDto?> GetByUserIdAsync(int userId);
    Task<IEnumerable<CompanyDto>> GetAllAsync();
    Task<CompanyDto> CreateAsync(int userId, CreateCompanyRequest request, IFormFile? avatar, IFormFile? coverImage);
    Task<CompanyDto> UpdateAsync(int id, UpdateCompanyRequest request, IFormFile? avatar, IFormFile? coverImage);
    Task<bool> DeleteAsync(int id);
}

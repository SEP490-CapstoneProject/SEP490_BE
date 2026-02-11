using Microsoft.AspNetCore.Http;
using UserProfile.Application.DTOs;
using UserProfile.Domain.Entities;

namespace UserProfile.Application.Interfaces;

public interface IEmployeeService
{
    Task<EmployeeDto?> GetByIdAsync(int id);
    Task<EmployeeDto?> GetByUserIdAsync(int userId);
    Task<IEnumerable<EmployeeDto>> GetAllAsync();
    Task<EmployeeDto> CreateAsync(int userId, CreateEmployeeRequest request, IFormFile? avatar, IFormFile? coverImage);
    Task<EmployeeDto> UpdateAsync(int id, UpdateEmployeeRequest request, IFormFile? avatar, IFormFile? coverImage);
    Task<bool> DeleteAsync(int id);
}

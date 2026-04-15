using Microsoft.AspNetCore.Http;
using UserProfile.Application.DTOs;

namespace UserProfile.Application.Interfaces;

public interface IExpertService
{
    Task<ExpertDto?> GetByIdAsync(int id);
    Task<ExpertDto?> GetByUserIdAsync(int userId);
    Task<IEnumerable<ExpertDto>> GetAllAsync();
    Task<ExpertDto> CreateAsync(int userId, CreateExpertRequest request, IFormFile? avatar, IFormFile? coverImage);
    Task<ExpertDto> UpdateAsync(int id, UpdateExpertRequest request, IFormFile? avatar, IFormFile? coverImage);
    Task<bool> DeleteAsync(int id);
}

using Portfolio.Application.DTOs;

namespace Portfolio.Application.Interfaces;

public interface ICriterionService
{
    Task<List<CriterionDto>> GetAllAsync();
    Task<List<CriterionDto>> GetActiveAsync();
    Task<CriterionDto> GetByIdAsync(int id);
    Task<CriterionDto> CreateAsync(CreateCriterionRequest request);
    Task<CriterionDto> UpdateAsync(int id, UpdateCriterionRequest request);
    Task<CriterionDto> ToggleActiveAsync(int id);
    Task DeleteAsync(int id);
}

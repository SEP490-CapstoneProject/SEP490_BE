using Portfolio.Application.DTOs;

namespace Portfolio.Application.Interfaces;

public interface IComplimentService
{
    Task<ComplimentDto> CreateAsync(CreateComplimentRequest request);
    Task<List<ComplimentDto>> GetByPortfolioAsync(int portfolioId);
    Task<ComplimentDto> UpdateAsync(int id, UpdateComplimentRequest request);
    Task<ComplimentDto> PatchStateAsync(int id, PatchComplimentStateRequest request);
    Task DeleteAsync(int id);
}

using Microsoft.AspNetCore.Http;
using Portfolio.Application.DTOs;

namespace Portfolio.Application.Interfaces;

public interface IPortfolioService
{
    Task<PortfolioDto?> GetByIdAsync(int id);
    Task<IEnumerable<PortfolioDto>> GetByEmployeeIdAsync(int employeeId);
    Task<PortfolioDto> CreateAsync(int employeeId, CreatePortfolioRequest request);
    Task<PortfolioDto> UpdateAsync(int id, int employeeId, UpdatePortfolioRequest request);
    Task<bool> DeleteAsync(int id, int employeeId);
    Task<CreatePortfolioResponse> CreatePortfolioAsync(CreatePortfolioRequest request, Dictionary<string, IFormFile> fileMap);
}

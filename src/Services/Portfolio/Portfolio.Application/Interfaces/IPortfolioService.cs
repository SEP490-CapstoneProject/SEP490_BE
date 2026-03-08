using Microsoft.AspNetCore.Http;
using Portfolio.Application.DTOs;

namespace Portfolio.Application.Interfaces;

public interface IPortfolioService
{
    Task<PortfolioDto?> GetByIdAsync(int id);
    Task<IEnumerable<PortfolioDto>> GetByEmployeeIdAsync(int employeeId);
    Task<PagedResult<PortfolioDto>> GetAllAsync(int page, int pageSize, string? status);
    Task<PortfolioDto> CreateAsync(int employeeId, CreatePortfolioRequest request);
    Task<PortfolioDto> UpdateAsync(int id, int employeeId, UpdatePortfolioRequest request);
    Task<CreatePortfolioResponse> UpdateFullPortfolioAsync(int id, int employeeId, UpdateFullPortfolioRequest request, Dictionary<string, IFormFile> fileMap);
    Task<bool> DeleteAsync(int id, int employeeId);
    Task<CreatePortfolioResponse> CreatePortfolioAsync(CreatePortfolioRequest request, Dictionary<string, IFormFile> fileMap);
}

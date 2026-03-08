using Portfolio.Domain.Entities;

namespace Portfolio.Application.Interfaces;

public interface IPortfolioRepository
{
    Task<Portfolio.Domain.Entities.Portfolio?> GetByIdAsync(int id);
    Task<IEnumerable<Portfolio.Domain.Entities.Portfolio>> GetByEmployeeIdAsync(int employeeId);
    Task<(List<Portfolio.Domain.Entities.Portfolio> Items, int Total)> GetAllAsync(int page, int pageSize, string? status);
    Task<bool> ExistsByEmployeeIdAsync(int employeeId);
    Task<Portfolio.Domain.Entities.Portfolio> CreateAsync(Portfolio.Domain.Entities.Portfolio portfolio);
    Task<Portfolio.Domain.Entities.Portfolio> UpdateAsync(Portfolio.Domain.Entities.Portfolio portfolio);
    Task<bool> DeleteAsync(int id);
    void AddAsync(Portfolio.Domain.Entities.Portfolio portfolio);
    Task CommitAsync();
    Task<Dictionary<string, BlockType>> GetBlockTypesAsync();
}

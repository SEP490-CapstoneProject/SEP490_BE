using Portfolio.Domain.Entities;

namespace Portfolio.Application.Interfaces;

public interface IBlockRepository
{
    Task<PortfolioBlock?> GetByIdWithDataAsync(int blockId);
    Task<List<PortfolioBlock>> GetByPortfolioIdAsync(int portfolioId);
    Task<int> GetMaxOrderAsync(int portfolioId);
    Task<int> CountByTypeAsync(int portfolioId, int blockTypeId);
    Task<PortfolioBlock> CreateAsync(PortfolioBlock block);
    Task<PortfolioBlock> UpdateAsync(PortfolioBlock block);
    Task DeleteAsync(PortfolioBlock block);
    Task ReorderAsync(List<(int blockId, int order)> reorders);
}

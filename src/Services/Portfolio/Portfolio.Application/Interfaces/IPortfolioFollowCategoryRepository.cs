using Portfolio.Domain.Entities;

namespace Portfolio.Application.Interfaces;

public interface IPortfolioFollowCategoryRepository
{
    Task<PortfolioFollowCategory?> GetByIdAsync(int categoryId);
    Task<PortfolioFollowCategory?> GetByCompanyAndCodeAsync(int companyId, string code);
    Task<List<PortfolioFollowCategory>> GetByCompanyAsync(int companyId);
    Task<bool> IsCategoryInUseAsync(int categoryId);
    Task<PortfolioFollowCategory> CreateAsync(PortfolioFollowCategory category);
    Task<PortfolioFollowCategory> UpdateAsync(PortfolioFollowCategory category);
    Task DeleteAsync(PortfolioFollowCategory category);
}

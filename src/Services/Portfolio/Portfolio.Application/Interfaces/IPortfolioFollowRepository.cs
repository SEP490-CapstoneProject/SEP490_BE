using Portfolio.Domain.Entities;

namespace Portfolio.Application.Interfaces;

public interface IPortfolioFollowRepository
{
    Task<PortfolioFollow?> GetByCompanyAndPortfolioAsync(int companyId, int portfolioId);
    Task<List<PortfolioFollow>> GetByCompanyAsync(int companyId);
    Task<PortfolioFollow> CreateAsync(PortfolioFollow follow);
    Task<PortfolioFollow> UpdateAsync(PortfolioFollow follow);
    Task DeleteAsync(PortfolioFollow follow);
}

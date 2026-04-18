using Portfolio.Application.DTOs;

namespace Portfolio.Application.Interfaces;

public interface IPortfolioFollowService
{
    Task<PortfolioFollowDto> CreateAsync(CreatePortfolioFollowRequest request);
    Task<List<PortfolioFollowDto>> GetMyFollowsAsync(int? categoryId = null);
    Task<PortfolioFollowDto> UpdateInterestAsync(int portfolioId, UpdatePortfolioFollowRequest request);
    Task DeleteAsync(int portfolioId);
}

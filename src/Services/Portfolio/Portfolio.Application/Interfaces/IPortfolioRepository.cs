using Portfolio.Application.DTOs;
using Portfolio.Domain.Entities;

namespace Portfolio.Application.Interfaces;

public interface IPortfolioRepository
{
    Task<Portfolio.Domain.Entities.Portfolio?> GetByIdAsync(int id);
    Task<IEnumerable<Portfolio.Domain.Entities.Portfolio>> GetByEmployeeIdAsync(int employeeId);
    Task<Portfolio.Domain.Entities.Portfolio?> GetMainByEmployeeIdAsync(int employeeId);
    Task<(List<Portfolio.Domain.Entities.Portfolio> Items, int Total, Dictionary<int, (decimal TotalScore, decimal AverageScore, int RankPosition)> RankingMap)> GetAllAsync(int page, int pageSize, string? status, string? searchTerm, string? blockType, PortfolioSortMode sort, PortfolioRankBy rankBy);
    Task<(List<Portfolio.Domain.Entities.Portfolio> Items, int Total)> GetPendingForModerationAsync(int page, int pageSize);
    Task<bool> ExistsByEmployeeIdAsync(int employeeId);
    Task<Portfolio.Domain.Entities.Portfolio> CreateAsync(Portfolio.Domain.Entities.Portfolio portfolio);
    Task<Portfolio.Domain.Entities.Portfolio> UpdateAsync(Portfolio.Domain.Entities.Portfolio portfolio);
    Task SetMainPortfolioAsync(int employeeId, int portfolioId);
    Task<bool> DeleteAsync(int id);
    void AddAsync(Portfolio.Domain.Entities.Portfolio portfolio);
    Task CommitAsync();
    Task<Dictionary<string, BlockType>> GetBlockTypesAsync();
    Task<Dictionary<int, List<int>>> GetReviewerUserIdsByPortfolioIdsAsync(IEnumerable<int> portfolioIds);
    Task<(List<PortfolioWithComplimentDto> Items, int Total)> GetAllWithComplimentFilterAsync(PortfolioQueryParams queryParams);
    Task<List<Portfolio.Domain.Entities.Portfolio>> GetPublicPortfoliosForMatchingAsync(int limit);
    Task<List<Portfolio.Domain.Entities.Portfolio>> GetPortfoliosForEmbeddingBackfillAsync(int limit);
    Task UpdateEmbeddingAsync(int portfolioId, string? embedding, int embeddingVersion, DateTime? embeddingUpdatedAt, string embeddingStatus);
    Task<PortfolioReport> CreatePortfolioReportAsync(PortfolioReport report);
    Task<PortfolioReport?> GetPortfolioReportByIdAndReporterAsync(int portfolioId, int reporterUserId);
    Task<(List<PortfolioReport> Items, int Total)> GetPortfolioReportsAsync(int page, int pageSize);
    Task<PortfolioReport?> GetPortfolioReportByIdAsync(int reportId);
    Task UpdatePortfolioReportAsync(PortfolioReport report);
}

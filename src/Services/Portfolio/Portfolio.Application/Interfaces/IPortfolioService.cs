using Microsoft.AspNetCore.Http;
using Portfolio.Application.DTOs;
using Portfolio.Domain.Entities;

namespace Portfolio.Application.Interfaces;

public interface IPortfolioService
{
    Task<PortfolioDto?> GetByIdAsync(int id);
    Task<IEnumerable<PortfolioDto>> GetByEmployeeIdAsync(int employeeId);
    Task<PortfolioDto?> GetMainByEmployeeIdAsync(int employeeId);
    Task<PagedResult<PortfolioDto>> GetAllAsync(int page, int pageSize, string? status, string? searchTerm, string? blockType, PortfolioSortMode sort, PortfolioRankBy rankBy);
    Task<PortfolioDto> CreateAsync(int employeeId, CreatePortfolioRequest request);
    Task<PortfolioDto> UpdateAsync(int id, int employeeId, UpdatePortfolioRequest request);
    Task<PortfolioDto> ToggleMainAsync(int id, int employeeId);
    Task<PortfolioDto> TogglePublicAsync(int id, int employeeId);
    Task<CreatePortfolioResponse> UpdateFullPortfolioAsync(int id, int employeeId, UpdateFullPortfolioRequest request, Dictionary<string, IFormFile> fileMap);
    Task<bool> DeleteAsync(int id, int employeeId);
    Task<CreatePortfolioResponse> CreatePortfolioAsync(CreatePortfolioRequest request, Dictionary<string, IFormFile> fileMap);
    Task<PagedResult<PortfolioDto>> GetPendingPortfoliosAsync(int page, int pageSize);
    Task<PortfolioDto> ApprovePortfolioAsync(int portfolioId, int reviewerId, string actorRole, string? notes);
    Task<PortfolioDto> RejectPortfolioAsync(int portfolioId, int reviewerId, string actorRole, string reason);
    Task<PagedResult<PortfolioWithComplimentDto>> GetAllWithComplimentFilterAsync(PortfolioQueryParams queryParams);
    Task<JobMatchPagedResult> MatchJobsForPortfolioAsync(int portfolioId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<MatchingCandidateFeed> GetMatchingCandidatesAsync(int limit, CancellationToken cancellationToken = default);
    Task<PortfolioReportDto> ReportPortfolioAsync(int portfolioId, int reporterUserId, CreatePortfolioReportRequest request);
    Task<PagedResult<PortfolioReportDto>> GetPortfolioReportsAsync(int page, int pageSize);
    Task<PortfolioReportDto> ReviewPortfolioReportAsync(int reportId, int reviewerUserId, ReviewPortfolioReportRequest request);
}

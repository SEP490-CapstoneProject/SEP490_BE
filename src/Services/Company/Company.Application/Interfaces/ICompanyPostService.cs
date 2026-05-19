using Company.Application.DTOs;
using Company.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace Company.Application.Interfaces;

public interface ICompanyPostService
{
    Task<CursorPagedResult<CompanyPostFeedDto>> GetPostFeedAsync(DateTime? cursor, int limit, int? userId);
    Task<CursorPagedResult<CompanyPostFeedDto>> GetPostsByCompanyAsync(int companyId, DateTime? cursor, int limit, int? userId);
    Task<CursorPagedResult<CompanyPostFeedDto>> GetSavedPostsAsync(DateTime? cursor, int limit, int userId);
    Task<List<CompanyPostDetailDto>> GetPostsByIdsAsync(List<int> postIds, int? userId);
    Task<CompanyPostDetailDto?> GetPostDetailAsync(int postId, int? userId);
    Task<CompanyPostDetailDto> CreatePostAsync(CreatePostRequest request, int companyId, Dictionary<string, IFormFile> fileMap);
    Task<CompanyPostDetailDto?> UpdatePostAsync(int postId, UpdatePostRequest request, int requesterId);
    Task<CompanyPostDetailDto?> UpdatePostFullAsync(int postId, UpdatePostFullRequest request, int requesterId, Dictionary<string, IFormFile> fileMap);
    Task<bool> SoftDeletePostAsync(int postId, int requesterId);
    Task SavePostAsync(int postId, int userId);
    Task UnsavePostAsync(int postId, int userId);
    Task<PortfolioMatchPagedResult> MatchPortfoliosForJobAsync(int postId, int page, int pageSize, CancellationToken cancellationToken = default);
    Task<MatchingCandidateFeed> GetMatchingCandidatesAsync(int limit, CancellationToken cancellationToken = default);
    
    // Admin moderation operations
    Task<List<CompanyPostDetailDto>> GetPendingPostsAsync(int skip, int take);
    Task<CompanyPost> ApprovePostAsync(int postId, string? notes);
    Task<CompanyPost> RejectPostAsync(int postId, string reason);

    // Report operations
    Task<CompanyPostReportDto> ReportPostAsync(int postId, int reporterUserId, CreatePostReportRequest request);
    Task<PagedResult<CompanyPostReportDto>> GetPostReportsAsync(int page, int pageSize);
    Task<CompanyPostReportDto> ReviewPostReportAsync(int reportId, int reviewerUserId, ReviewPostReportRequest request);
}


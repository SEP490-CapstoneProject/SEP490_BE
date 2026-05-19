using Company.Application.DTOs;
using Company.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace Company.Application.Interfaces;

public interface ICompanyPostRepository
{
    Task<CursorPagedResult<CompanyPostFeedDto>> GetPostFeedAsync(DateTime? cursor, int limit, int? userId);
    Task<CursorPagedResult<CompanyPostFeedDto>> GetPostsByCompanyAsync(int companyId, DateTime? cursor, int limit, int? userId);
    Task<CursorPagedResult<CompanyPostFeedDto>> GetSavedPostsAsync(DateTime? cursor, int limit, int userId);
    Task<List<CompanyPostDetailDto>> GetPostsByIdsAsync(List<int> postIds, int? userId);
    Task<CompanyPostDetailDto?> GetPostDetailAsync(int postId, int? userId);
    Task<CompanyPost?> GetPostEntityByIdAsync(int postId);
    Task<CompanyPost?> GetByIdAsync(int postId);
    Task<List<CompanyPost>> GetActivePostsForMatchingAsync(int limit);
    Task<List<CompanyPost>> GetPostsForEmbeddingBackfillAsync(int limit);
    Task<List<CompanyPost>> GetPendingPostsAsync(int pageNumber, int pageSize);
    Task UpdateEmbeddingAsync(int postId, string? embedding, int embeddingVersion, DateTime? embeddingUpdatedAt, string embeddingStatus);
    Task<bool> CheckPostSavedAsync(int userId, int postId);
    Task SavePostAsync(int userId, int postId);
    Task UnsavePostAsync(int userId, int postId);
    Task<CompanyPost> CreatePostAsync(CompanyPost post);
    Task UpdatePostAsync(CompanyPost post);
    Task RemovePostMediaAsync(int postId);
    Task SoftDeletePostAsync(int postId);
    Task AddPostMediaAsync(CompanyPostMedia media);

    // Report operations
    Task<CompanyPostReport> CreatePostReportAsync(CompanyPostReport report);
    Task<CompanyPostReport?> GetPostReportByPostAndReporterAsync(int postId, int reporterUserId);
    Task<(List<CompanyPostReport> Items, int Total)> GetPostReportsAsync(int page, int pageSize);
    Task<CompanyPostReport?> GetPostReportByIdAsync(int reportId);
    Task UpdatePostReportAsync(CompanyPostReport report);
}


using Company.Application.DTOs;
using Microsoft.AspNetCore.Http;

namespace Company.Application.Interfaces;

public interface ICompanyPostService
{
    Task<CursorPagedResult<CompanyPostFeedDto>> GetPostFeedAsync(DateTime? cursor, int limit, int? userId);
    Task<CursorPagedResult<CompanyPostFeedDto>> GetPostsByCompanyAsync(int companyId, DateTime? cursor, int limit, int? userId);
    Task<CompanyPostDetailDto?> GetPostDetailAsync(int postId, int? userId);
    Task<CompanyPostDetailDto> CreatePostAsync(CreatePostRequest request, int companyId, Dictionary<string, IFormFile> fileMap);
    Task<CompanyPostDetailDto?> UpdatePostAsync(int postId, UpdatePostRequest request, int requesterId);
    Task<bool> SoftDeletePostAsync(int postId, int requesterId);
    Task SavePostAsync(int postId, int userId);
    Task UnsavePostAsync(int postId, int userId);
}


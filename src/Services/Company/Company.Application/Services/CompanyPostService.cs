using Company.Application.Clients;
using Company.Application.DTOs;
using Company.Application.Interfaces;
using Company.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Company.Application.Services;

public class CompanyPostService : ICompanyPostService
{
    private readonly ICompanyPostRepository _repository;
    private readonly IMediaUploadClient _mediaUploadClient;
    private readonly ILogger<CompanyPostService> _logger;

    public CompanyPostService(
        ICompanyPostRepository repository,
        IMediaUploadClient mediaUploadClient,
        ILogger<CompanyPostService> logger)
    {
        _repository = repository;
        _mediaUploadClient = mediaUploadClient;
        _logger = logger;
    }

    public Task<CursorPagedResult<CompanyPostFeedDto>> GetPostFeedAsync(DateTime? cursor, int limit, int? userId)
        => _repository.GetPostFeedAsync(cursor, limit, userId);

    public Task<CursorPagedResult<CompanyPostFeedDto>> GetPostsByCompanyAsync(int companyId, DateTime? cursor, int limit, int? userId)
        => _repository.GetPostsByCompanyAsync(companyId, cursor, limit, userId);

    public Task<CompanyPostDetailDto?> GetPostDetailAsync(int postId, int? userId)
        => _repository.GetPostDetailAsync(postId, userId);

    public async Task<CompanyPostDetailDto> CreatePostAsync(CreatePostRequest request, int companyId, Dictionary<string, IFormFile> fileMap)
    {
        var post = new CompanyPost
        {
            CompanyId = companyId,
            Position = request.Position,
            Address = request.Address,
            Salary = request.Salary,
            EmploymentType = request.EmploymentType,
            ExperienceYear = request.ExperienceYear,
            Quantity = request.Quantity,
            JobDescription = request.JobDescription,
            RequirementsMandatory = request.RequirementsMandatory,
            RequirementsPreferred = request.RequirementsPreferred,
            Benefits = request.Benefits,
            Status = request.Status,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _repository.CreatePostAsync(post);

        // Upload cover image first
        if (!string.IsNullOrEmpty(request.CoverImageKey) &&
            fileMap.TryGetValue(request.CoverImageKey, out var coverFile))
        {
            var coverResult = await _mediaUploadClient.UploadAsync(coverFile, "company/posts/cover");
            if (coverResult != null)
            {
                created.CoverImageVideo = coverResult.Url;
                await _repository.UpdatePostAsync(created);
            }
            else
            {
                _logger.LogWarning("Cover image upload failed for post {PostId}", created.PostId);
            }
        }

        // Upload remaining media files
        foreach (var (filename, file) in fileMap)
        {
            if (!string.IsNullOrEmpty(request.CoverImageKey) &&
                string.Equals(filename, request.CoverImageKey, StringComparison.OrdinalIgnoreCase))
                continue;

            var result = await _mediaUploadClient.UploadAsync(file, "company/posts");
            if (result == null)
            {
                _logger.LogWarning("Media upload failed for file {Filename} on post {PostId}", filename, created.PostId);
                continue;
            }

            var mediaType = file.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase) ? "video" : "image";
            await _repository.AddPostMediaAsync(new CompanyPostMedia
            {
                CompanyPostId = created.PostId,
                Type = mediaType,
                Name = result.PublicId ?? filename,
                Address = result.Url
            });
        }

        var detail = await _repository.GetPostDetailAsync(created.PostId, companyId);
        return detail!;
    }

    public async Task<CompanyPostDetailDto?> UpdatePostAsync(int postId, UpdatePostRequest request, int requesterId)
    {
        var detail = await _repository.GetPostDetailAsync(postId, null);
        if (detail == null || detail.CompanyId != requesterId) return null;

        var post = new CompanyPost
        {
            PostId = postId,
            CompanyId = detail.CompanyId,
            Position = request.Position ?? detail.Position,
            Address = request.Address ?? detail.Address,
            Salary = request.Salary ?? detail.Salary,
            EmploymentType = request.EmploymentType ?? detail.EmploymentType,
            ExperienceYear = request.ExperienceYear ?? detail.ExperienceYear,
            Quantity = request.Quantity ?? detail.Quantity,
            JobDescription = request.JobDescription ?? detail.JobDescription,
            RequirementsMandatory = request.RequirementsMandatory ?? detail.RequirementsMandatory,
            RequirementsPreferred = request.RequirementsPreferred ?? detail.RequirementsPreferred,
            Benefits = request.Benefits ?? detail.Benefits,
            Status = request.Status ?? detail.Status,
            CoverImageVideo = detail.CoverImageUrl,
            CreatedAt = detail.CreatedAt
        };

        await _repository.UpdatePostAsync(post);
        return await _repository.GetPostDetailAsync(postId, requesterId);
    }

    public async Task<bool> SoftDeletePostAsync(int postId, int requesterId)
    {
        var detail = await _repository.GetPostDetailAsync(postId, null);
        if (detail == null || detail.CompanyId != requesterId) return false;

        await _repository.SoftDeletePostAsync(postId);
        return true;
    }

    public Task SavePostAsync(int postId, int userId)
        => _repository.SavePostAsync(userId, postId);

    public Task UnsavePostAsync(int postId, int userId)
        => _repository.UnsavePostAsync(userId, postId);
}


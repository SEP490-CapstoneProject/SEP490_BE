using Company.Application.Clients;
using Company.Application.DTOs;
using Company.Application.Helpers;
using Company.Application.Interfaces;
using Company.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Company.Application.Services;

public class CompanyPostService : ICompanyPostService
{
    private readonly ICompanyPostRepository _repository;
    private readonly ICompanyCacheRepository _companyCacheRepository;
    private readonly ICompanyProfileClient _companyProfileClient;
    private readonly IMediaUploadClient _mediaUploadClient;
    private readonly ILogger<CompanyPostService> _logger;

    public CompanyPostService(
        ICompanyPostRepository repository,
        ICompanyCacheRepository companyCacheRepository,
        ICompanyProfileClient companyProfileClient,
        IMediaUploadClient mediaUploadClient,
        ILogger<CompanyPostService> logger)
    {
        _repository = repository;
        _companyCacheRepository = companyCacheRepository;
        _companyProfileClient = companyProfileClient;
        _mediaUploadClient = mediaUploadClient;
        _logger = logger;
    }

    public async Task<CursorPagedResult<CompanyPostFeedDto>> GetPostFeedAsync(DateTime? cursor, int limit, int? userId)
    {
        var result = await _repository.GetPostFeedAsync(cursor, limit, userId);
        await EnrichCompanyCacheAsync(result.Items);
        return result;
    }

    public async Task<CursorPagedResult<CompanyPostFeedDto>> GetPostsByCompanyAsync(int companyId, DateTime? cursor, int limit, int? userId)
    {
        var result = await _repository.GetPostsByCompanyAsync(companyId, cursor, limit, userId);
        await EnrichCompanyCacheAsync(result.Items);
        return result;
    }

    public async Task<CursorPagedResult<CompanyPostFeedDto>> GetSavedPostsAsync(DateTime? cursor, int limit, int userId)
    {
        var result = await _repository.GetSavedPostsAsync(cursor, limit, userId);
        await EnrichCompanyCacheAsync(result.Items);
        return result;
    }

    public async Task<List<CompanyPostDetailDto>> GetPostsByIdsAsync(List<int> postIds, int? userId)
    {
        return await _repository.GetPostsByIdsAsync(postIds, userId);
    }

    public async Task<CompanyPostDetailDto?> GetPostDetailAsync(int postId, int? userId)
    {
        var detail = await _repository.GetPostDetailAsync(postId, userId);
        if (detail == null) return null;

        await EnrichCompanyCacheAsync(detail);
        return detail;
    }

    public async Task<CompanyPostDetailDto> CreatePostAsync(CreatePostRequest request, int companyId, Dictionary<string, IFormFile> fileMap)
    {
        var profile = await _companyProfileClient.GetCompanyAsync(companyId);
        if (profile == null)
        {
            throw new InvalidOperationException("Company profile not found.");
        }

        await _companyCacheRepository.UpsertAsync(new CompanyEntity
        {
            Id = profile.Id,
            Name = profile.CompanyName,
            AvatarUrl = profile.Avatar
        });

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
            CreatedAt = DateTimeHelper.GetVietnamTime()
        };

        var created = await _repository.CreatePostAsync(post);

        await UploadPostMediaAsync(created, request.CoverImageKey, fileMap, replaceAllMedia: false);
        await _repository.UpdatePostAsync(created);

        var detail = await _repository.GetPostDetailAsync(created.PostId, companyId);
        if (detail != null)
        {
            detail.CompanyName = profile.CompanyName;
            detail.CompanyAvatar = profile.Avatar;
        }
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

    public async Task<CompanyPostDetailDto?> UpdatePostFullAsync(int postId, UpdatePostFullRequest request, int requesterId, Dictionary<string, IFormFile> fileMap)
    {
        var detail = await _repository.GetPostDetailAsync(postId, null);
        if (detail == null || detail.CompanyId != requesterId) return null;

        if (string.IsNullOrWhiteSpace(request.Position))
        {
            throw new InvalidOperationException("Position is required.");
        }

        var post = new CompanyPost
        {
            PostId = postId,
            CompanyId = detail.CompanyId,
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
            CoverImageVideo = detail.CoverImageUrl,
            CreatedAt = detail.CreatedAt
        };

        await UploadPostMediaAsync(post, request.CoverImageKey, fileMap, replaceAllMedia: true);
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

    private async Task EnrichCompanyCacheAsync(List<CompanyPostFeedDto> items)
    {
        var missingIds = items
            .Where(i => i.CompanyId > 0 && (string.IsNullOrEmpty(i.CompanyName) || string.IsNullOrEmpty(i.CompanyAvatar)))
            .Select(i => i.CompanyId)
            .Distinct()
            .ToList();

        if (missingIds.Count == 0) return;

        var profiles = await _companyProfileClient.GetCompaniesAsync(missingIds);
        if (profiles.Count == 0) return;

        await _companyCacheRepository.UpsertRangeAsync(profiles.Select(p => new CompanyEntity
        {
            Id = p.Id,
            Name = p.CompanyName,
            AvatarUrl = p.Avatar
        }));

        var lookup = profiles.ToDictionary(p => p.Id);
        foreach (var item in items)
        {
            if (lookup.TryGetValue(item.CompanyId, out var profile))
            {
                item.CompanyName = profile.CompanyName;
                item.CompanyAvatar = profile.Avatar;
            }
        }
    }

    private async Task EnrichCompanyCacheAsync(CompanyPostDetailDto detail)
    {
        if (detail.CompanyId <= 0) return;
        if (!string.IsNullOrEmpty(detail.CompanyName) || !string.IsNullOrEmpty(detail.CompanyAvatar)) return;

        var profile = await _companyProfileClient.GetCompanyAsync(detail.CompanyId);
        if (profile == null) return;

        await _companyCacheRepository.UpsertAsync(new CompanyEntity
        {
            Id = profile.Id,
            Name = profile.CompanyName,
            AvatarUrl = profile.Avatar
        });

        detail.CompanyName = profile.CompanyName;
        detail.CompanyAvatar = profile.Avatar;
    }

    private async Task UploadPostMediaAsync(CompanyPost post, string? coverImageKey, Dictionary<string, IFormFile> fileMap, bool replaceAllMedia)
    {
        if (!string.IsNullOrWhiteSpace(coverImageKey) &&
            fileMap.TryGetValue(coverImageKey, out var coverFile))
        {
            var coverResult = await _mediaUploadClient.UploadAsync(coverFile, "company/posts/cover");
            if (coverResult != null)
            {
                post.CoverImageVideo = coverResult.Url;
            }
            else
            {
                _logger.LogWarning("Cover image upload failed for post {PostId}", post.PostId);
            }
        }

        if (replaceAllMedia)
        {
            await _repository.RemovePostMediaAsync(post.PostId);
        }

        foreach (var (filename, file) in fileMap)
        {
            if (!string.IsNullOrWhiteSpace(coverImageKey) &&
                string.Equals(filename, coverImageKey, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var result = await _mediaUploadClient.UploadAsync(file, "company/posts");
            if (result == null)
            {
                _logger.LogWarning("Media upload failed for file {Filename} on post {PostId}", filename, post.PostId);
                continue;
            }

            var mediaType = file.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase) ? "video" : "image";
            await _repository.AddPostMediaAsync(new CompanyPostMedia
            {
                CompanyPostId = post.PostId,
                Type = mediaType,
                Name = result.PublicId ?? filename,
                Address = result.Url
            });
        }
    }
}


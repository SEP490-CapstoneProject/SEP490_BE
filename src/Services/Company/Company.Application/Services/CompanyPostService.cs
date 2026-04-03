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
}


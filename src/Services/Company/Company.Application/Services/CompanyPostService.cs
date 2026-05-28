using Company.Application.Clients;
using Company.Application.DTOs;
using Company.Application.Helpers;
using Company.Application.Interfaces;
using Company.Application.Models.Events;
using Company.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using RecruitmentPlatform.AI.Abstractions;
using RecruitmentPlatform.AI.Models;
using RecruitmentPlatform.AI.Services;
using RecruitmentPlatform.Contracts.Realtime;
using Company.Domain.Enums;
using System.Text.Json;

namespace Company.Application.Services;

public class CompanyPostService : ICompanyPostService
{
    private readonly ICompanyPostRepository _repository;
    private readonly ICompanyCacheRepository _companyCacheRepository;
    private readonly ICompanyProfileClient _companyProfileClient;
    private readonly IMediaUploadClient _mediaUploadClient;
    private readonly IPortfolioMatchingClient _portfolioMatchingClient;
    private readonly ICompanyEmbeddingEventPublisher _embeddingEventPublisher;
    private readonly ICompanyNotificationEventPublisher _notificationPublisher;
    private readonly IMatchingEngine _matchingEngine;
    private readonly ITextNormalizer _textNormalizer;
    private readonly IEmbeddingService _embeddingService;
    private readonly IMemoryCache _cache;
    private readonly ModerationService _moderationService;
    private readonly ILogger<CompanyPostService> _logger;

    public CompanyPostService(
        ICompanyPostRepository repository,
        ICompanyCacheRepository companyCacheRepository,
        ICompanyProfileClient companyProfileClient,
        IMediaUploadClient mediaUploadClient,
        IPortfolioMatchingClient portfolioMatchingClient,
        ICompanyEmbeddingEventPublisher embeddingEventPublisher,
        ICompanyNotificationEventPublisher notificationPublisher,
        IMatchingEngine matchingEngine,
        ITextNormalizer textNormalizer,
        IEmbeddingService embeddingService,
        IMemoryCache cache,
        ModerationService moderationService,
        ILogger<CompanyPostService> logger)
    {
        _repository = repository;
        _companyCacheRepository = companyCacheRepository;
        _companyProfileClient = companyProfileClient;
        _mediaUploadClient = mediaUploadClient;
        _portfolioMatchingClient = portfolioMatchingClient;
        _embeddingEventPublisher = embeddingEventPublisher;
        _notificationPublisher = notificationPublisher;
        _matchingEngine = matchingEngine;
        _textNormalizer = textNormalizer;
        _embeddingService = embeddingService;
        _cache = cache;
        _moderationService = moderationService;
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

        // Combine job description content for moderation check
        var contentToCheck = string.Join(" ", new[] 
        { 
            request.Position ?? "",
            request.JobDescription ?? "",
            request.RequirementsMandatory ?? "",
            request.RequirementsPreferred ?? "",
            request.Benefits ?? ""
        }.Where(s => !string.IsNullOrWhiteSpace(s)));

        // Run moderation check before creating post
        var moderationResult = _moderationService.CheckPost(contentToCheck);

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

        // Set moderation fields based on check result
        if (moderationResult.Status == "Rejected")
        {
            post.Status = CompanyPost.StatusInactive;
            post.ReviewStatus = CompanyPost.StatusRejected;
            post.ReviewReason = moderationResult.Reason;
            post.ReviewedAt = DateTimeHelper.GetVietnamTime();
        }
        else if (moderationResult.Status == "PendingReview")
        {
            post.ReviewStatus = CompanyPost.StatusPendingReview;
            post.ReviewReason = moderationResult.Reason;
            post.ReviewedAt = DateTimeHelper.GetVietnamTime();
        }

        await UpdateEmbeddingStateAsync(post);

        var created = await _repository.CreatePostAsync(post);

        // Publish notifications based on moderation result
        if (created.ReviewStatus == CompanyPost.StatusRejected)
        {
            // Auto-rejected - notify company
            var evt = new PostRejectedNotificationEvent
            {
                EventId = Guid.NewGuid().ToString("N"),
                EventType = "post.rejected",
                Version = 1,
                UserId = created.CompanyId.ToString(),
                ActorId = "SYSTEM",
                ActorType = "SYSTEM",
                ObjectId = created.PostId.ToString(),
                Title = "bài đăng của bạn đã bị từ chối",
                Content = $"Bài đăng công việc của bạn đã bị từ chối. Lý do: {moderationResult.Reason}",
                Type = "POST_REJECTED",
                PostType = "Company",
                CreatedAt = DateTimeHelper.GetVietnamTime()
            };

            await _notificationPublisher.PublishPostRejectedNotificationAsync(evt);
        }
        else if (created.ReviewStatus == CompanyPost.StatusPendingReview)
        {
            // Needs manual review - notify company
            var evt = new PostPendingReviewNotificationEvent
            {
                EventId = Guid.NewGuid().ToString("N"),
                EventType = "post.pending.review",
                Version = 1,
                UserId = created.CompanyId.ToString(),
                ActorId = null,
                ActorType = "SYSTEM",
                ObjectId = created.PostId.ToString(),
                Title = "bài đăng của bạn đang được xem xét",
                Content = $"Bài đăng công việc của bạn đang chờ xem xét thủ công. Lý do: {moderationResult.Reason}",
                Type = "POST_PENDING_REVIEW",
                PostType = "Company",
                CreatedAt = DateTimeHelper.GetVietnamTime()
            };

            await _notificationPublisher.PublishPostPendingReviewNotificationAsync(evt);

            var triageEvt = new PostPendingReviewNotificationEvent
            {
                EventId = Guid.NewGuid().ToString("N"),
                EventType = "post.pending.review",
                Version = 1,
                UserId = string.Empty,
                ActorId = null,
                ActorType = "SYSTEM",
                ObjectId = created.PostId.ToString(),
                Title = "Bài đăng tuyển dụng chờ duyệt thủ công",
                Content = $"Bài đăng #{created.PostId} cần admin/moderator xem xét. Lý do: {moderationResult.Reason}",
                Type = "POST_PENDING_REVIEW",
                PostType = "Company",
                TargetRoles = new[] { "ADMIN", "MODERATOR" },
                CreatedAt = DateTimeHelper.GetVietnamTime()
            };

            await _notificationPublisher.PublishPostPendingReviewNotificationAsync(triageEvt);
        }
        else
        {
            // Auto-approved - notify company
            var evt = new PostApprovedNotificationEvent
            {
                EventId = Guid.NewGuid().ToString("N"),
                EventType = "post.approved",
                Version = 1,
                UserId = created.CompanyId.ToString(),
                ActorId = "SYSTEM",
                ActorType = "SYSTEM",
                ObjectId = created.PostId.ToString(),
                Title = "bài đăng của bạn đã được duyệt",
                Content = "Bài đăng công việc của bạn đã được duyệt và hiện đang hiển thị.",
                Type = "POST_APPROVED",
                PostType = "Company",
                ApproverNotes = "Auto-approved by content moderation system",
                CreatedAt = DateTimeHelper.GetVietnamTime()
            };

            await _notificationPublisher.PublishPostApprovedNotificationAsync(evt);
        }

        await UploadPostMediaAsync(created, request.CoverImageKey, fileMap, replaceAllMedia: false);
        await _repository.UpdatePostAsync(created);
        await TryPublishEmbeddingEventAsync(created.PostId);

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
        var existingPost = await _repository.GetPostEntityByIdAsync(postId);

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
            CreatedAt = detail.CreatedAt,
            Embedding = null,
            EmbeddingVersion = existingPost?.EmbeddingVersion ?? 0,
            EmbeddingStatus = EmbeddingReadinessPolicy.Pending
        };
        await UpdateEmbeddingStateAsync(post);

        await _repository.UpdatePostAsync(post);
        await TryPublishEmbeddingEventAsync(postId);
        return await _repository.GetPostDetailAsync(postId, requesterId);
    }

    public async Task<CompanyPostDetailDto?> UpdatePostFullAsync(int postId, UpdatePostFullRequest request, int requesterId, Dictionary<string, IFormFile> fileMap)
    {
        var detail = await _repository.GetPostDetailAsync(postId, null);
        if (detail == null || detail.CompanyId != requesterId) return null;
        var existingPost = await _repository.GetPostEntityByIdAsync(postId);

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
            CreatedAt = detail.CreatedAt,
            Embedding = null,
            EmbeddingVersion = existingPost?.EmbeddingVersion ?? 0,
            EmbeddingStatus = EmbeddingReadinessPolicy.Pending
        };
        await UpdateEmbeddingStateAsync(post);

        await UploadPostMediaAsync(post, request.CoverImageKey, fileMap, replaceAllMedia: true);
        await _repository.UpdatePostAsync(post);
        await TryPublishEmbeddingEventAsync(postId);
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

    public async Task<PagedResult<CompanyPostFeedDto>> SearchPostsAsync(
        string? q, string? position, string? salary, string? location, string? employmentType, string? level,
        string? q_position, string? q_description, string? q_requirements,
        int skip, int take, int? userId)
    {
        var result = await _repository.SearchPostsAsync(q, position, salary, location, employmentType, level, q_position, q_description, q_requirements, skip, take, userId);
        await EnrichCompanyCacheAsync(result.Items);
        return result;
    }

    public async Task<PortfolioMatchPagedResult> MatchPortfoliosForJobAsync(int postId, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        var (safePage, safePageSize) = NormalizeMatchPaging(page, pageSize);
        var post = await _repository.GetPostEntityByIdAsync(postId);
        if (post == null)
        {
            return new PortfolioMatchPagedResult { Page = safePage, PageSize = safePageSize };
        }

        var sourceEmbedding = ParseEmbedding(post.Embedding);
        if (!EmbeddingReadinessPolicy.IsReady(post.EmbeddingStatus, sourceEmbedding))
        {
            return new PortfolioMatchPagedResult { Page = safePage, PageSize = safePageSize };
        }

        var cacheKey = $"job:{post.PostId}:{post.EmbeddingVersion}:matched-portfolios:{safePage}:{safePageSize}";
        if (_cache.TryGetValue(cacheKey, out PortfolioMatchPagedResult? cached) && cached != null)
        {
            return cached;
        }

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(5));
        var candidates = await _portfolioMatchingClient.GetPortfolioCandidatesAsync(cts.Token);

        var request = new MatchingRequest
        {
            SourceId = post.PostId,
            SourceTitle = post.Position,
            SourceDescription = post.JobDescription ?? string.Empty,
            SourceSkills = ExtractSkills(post.RequirementsMandatory, post.RequirementsPreferred),
            SourceCategories = ExtractCategories(post.EmploymentType, post.Address),
            SourceEmbedding = sourceEmbedding,
            SourceEmbeddingVersion = post.EmbeddingVersion
        };

        var matches = _matchingEngine.Match(request, candidates, safePage, safePageSize);

        // Enrich with portfolio details
        var matchedIds = matches.Items.Select(x => x.Id).ToList();
        var portfolioDetails = await _portfolioMatchingClient.GetPortfoliosByIdsAsync(matchedIds, cancellationToken);
        var detailLookup = portfolioDetails.ToDictionary(p => p.PortfolioId);

        var result = new PortfolioMatchPagedResult
        {
            Total = matches.Total,
            Page = matches.Page,
            PageSize = matches.PageSize,
            Items = matches.Items.Select(x =>
            {
                detailLookup.TryGetValue(x.Id, out var detail);
                return new PortfolioMatchResultDto
                {
                    PortfolioId = x.Id,
                    Title = x.Title,
                    Cosine = x.Cosine,
                    SkillScore = x.SkillScore,
                    CategoryScore = x.CategoryScore,
                    FinalScore = x.FinalScore,
                    EmployeeId = detail?.EmployeeId ?? 0,
                    IsMain = detail?.IsMain ?? false,
                    IsPublic = detail?.IsPublic ?? false,
                    Status = detail?.Status ?? string.Empty,
                    ModerationStatus = detail?.ModerationStatus ?? string.Empty,
                    CreatedAt = detail?.CreatedAt ?? default,
                    UpdatedAt = detail?.UpdatedAt,
                    Blocks = detail?.Blocks ?? new()
                };
            }).ToList()
        };

        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(10));
        return result;
    }

    public async Task<MatchingCandidateFeed> GetMatchingCandidatesAsync(int limit, CancellationToken cancellationToken = default)
    {
        var posts = await _repository.GetActivePostsForMatchingAsync(limit);
        var items = posts
            .Select(post =>
            {
                var embedding = ParseEmbedding(post.Embedding);
                return new MatchingCandidate
                {
                    Id = post.PostId,
                    Title = post.Position,
                    Description = post.JobDescription ?? string.Empty,
                    Skills = ExtractSkills(post.RequirementsMandatory, post.RequirementsPreferred),
                    Categories = ExtractCategories(post.EmploymentType, post.Address),
                    Embedding = embedding,
                    EmbeddingVersion = post.EmbeddingVersion,
                    EmbeddingStatus = post.EmbeddingStatus,
                    UpdatedAt = post.EmbeddingUpdatedAt ?? post.CreatedAt
                };
            })
            .Where(candidate => candidate.Embedding.Length > 0)
            .ToList();

        return new MatchingCandidateFeed { Items = items };
    }

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

    private async Task UpdateEmbeddingStateAsync(CompanyPost post)
    {
        post.EmbeddingStatus = EmbeddingReadinessPolicy.Pending;
        var text = _textNormalizer.BuildJobText(new EmbeddingTextInput
        {
            Title = post.Position,
            Description = post.JobDescription,
            Skills = ExtractSkills(post.RequirementsMandatory, post.RequirementsPreferred),
            Categories = ExtractCategories(post.EmploymentType, post.Address),
            CustomFields = new[] { post.Benefits ?? "N/A", post.Salary ?? "N/A" }
        });

        try
        {
            var embedding = await _embeddingService.CreateEmbeddingAsync(text);
            post.Embedding = JsonSerializer.Serialize(embedding);
            post.EmbeddingVersion = Math.Max(1, post.EmbeddingVersion + 1);
            post.EmbeddingUpdatedAt = DateTimeHelper.GetVietnamTime();
            post.EmbeddingStatus = EmbeddingReadinessPolicy.ResolveStatus(embedding);
        }
        catch (Exception ex)
        {
            post.EmbeddingStatus = EmbeddingReadinessPolicy.Failed;
            _logger.LogWarning(ex, "Failed to generate embedding for company post {PostId}", post.PostId);
        }
    }

    private static float[] ParseEmbedding(string? embeddingJson)
    {
        if (string.IsNullOrWhiteSpace(embeddingJson))
        {
            return Array.Empty<float>();
        }

        try
        {
            return JsonSerializer.Deserialize<float[]>(embeddingJson) ?? Array.Empty<float>();
        }
        catch
        {
            return Array.Empty<float>();
        }
    }

    private static (int Page, int PageSize) NormalizeMatchPaging(int page, int pageSize)
    {
        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 50);
        return (safePage, safePageSize);
    }

    private static List<string> ExtractSkills(params string?[] textParts)
    {
        return textParts
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .SelectMany(x => x!.Split([',', ';', '\n', '\r', '|', '/'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(x => x.Length > 1)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<string> ExtractCategories(params string?[] textParts)
    {
        return textParts
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .SelectMany(x => x!.Split([',', ';', '\n', '\r', '|', '/'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Where(x => x.Length > 1)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private async Task TryPublishEmbeddingEventAsync(int postId)
    {
        try
        {
            await _embeddingEventPublisher.PublishCompanyPostChangedAsync(postId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish embedding event for company post {PostId}", postId);
        }
    }

    // ─── Admin moderation ─────────────────────────────────────────────────────

    public async Task<List<CompanyPostDetailDto>> GetPendingPostsAsync(int skip, int take)
    {
        if (take < 1) take = 20;
        if (take > 100) take = 100;

        // Get pending posts - need to filter by ReviewStatus = 3
        var pageNumber = (skip / take) + 1;
        var posts = await _repository.GetPendingPostsAsync(pageNumber, take + 1);
        
        var items = new List<CompanyPostDetailDto>();
        foreach (var post in posts)
        {
            var detail = await _repository.GetPostDetailAsync(post.PostId, null);
            if (detail != null)
            {
                items.Add(detail);
            }
        }

        return items;
    }

    public async Task<CompanyPost> ApprovePostAsync(int postId, string? notes)
    {
        var post = await _repository.GetByIdAsync(postId);
        if (post == null)
            throw new KeyNotFoundException($"Post not found");

        post.Status = CompanyPost.StatusActive;
        post.ReviewStatus = CompanyPost.StatusActive;
        post.ReviewReason = notes;
        post.ReviewedAt = DateTimeHelper.GetVietnamTime();

        await _repository.UpdatePostAsync(post);

        // Publish approval notification
        var evt = new PostApprovedNotificationEvent
        {
            EventId = Guid.NewGuid().ToString("N"),
            EventType = "post.approved",
            Version = 1,
            UserId = post.CompanyId.ToString(),
            ActorId = "ADMIN",
            ActorType = "ADMIN",
            ObjectId = post.PostId.ToString(),
            Title = "Your job post has been approved",
            Content = "Your company job post has been approved and is now live.",
            Type = "POST_APPROVED",
            PostType = "Company",
            ApproverNotes = notes,
            CreatedAt = DateTimeHelper.GetVietnamTime()
        };

        await _notificationPublisher.PublishPostApprovedNotificationAsync(evt);

        // Publish realtime event
        var realtimeEvt = new PostModerationEvent
        {
            EventId = Guid.NewGuid().ToString("N"),
            EventType = "post.moderation",
            Version = 1,
            PostId = post.PostId,
            UserId = post.CompanyId.ToString(),
            Status = "APPROVED",
            Reason = notes ?? "Post approved",
            PostType = "Company",
            Title = "Your job post has been approved",
            Content = "Your company job post has been approved and is now live.",
            ActorId = "SYSTEM",
            ActorType = "SYSTEM",
            CreatedAt = DateTimeHelper.GetVietnamTime()
        };

        await _embeddingEventPublisher.PublishCompanyPostChangedAsync(post.PostId);

        return post;
    }

    public async Task<CompanyPost> RejectPostAsync(int postId, string reason)
    {
        var post = await _repository.GetByIdAsync(postId);
        if (post == null)
            throw new KeyNotFoundException($"Post not found");

        post.Status = CompanyPost.StatusInactive;
        post.ReviewStatus = CompanyPost.StatusRejected;
        post.ReviewReason = reason;
        post.ReviewedAt = DateTimeHelper.GetVietnamTime();

        await _repository.UpdatePostAsync(post);

        // Publish rejection notification
        var evt = new PostRejectedNotificationEvent
        {
            EventId = Guid.NewGuid().ToString("N"),
            EventType = "post.rejected",
            Version = 1,
            UserId = post.CompanyId.ToString(),
            ActorId = "ADMIN",
            ActorType = "ADMIN",
            ObjectId = post.PostId.ToString(),
            Title = "Your job post was rejected",
            Content = $"Your company job post was rejected. Reason: {reason}",
            Type = "POST_REJECTED",
            PostType = "Company",
            CreatedAt = DateTimeHelper.GetVietnamTime()
        };

        await _notificationPublisher.PublishPostRejectedNotificationAsync(evt);

        return post;
    }

    public async Task<CompanyPostReportDto> ReportPostAsync(int postId, int reporterUserId, CreatePostReportRequest request)
    {
        var post = await _repository.GetByIdAsync(postId)
            ?? throw new KeyNotFoundException($"Post {postId} not found");

        if (post.CompanyId == reporterUserId)
        {
            throw new InvalidOperationException("You cannot report your own post.");
        }

        var reason = request.Reason?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("Reason is required.");
        }

        if (reason.Length > 100)
        {
            throw new ArgumentException("Reason must not exceed 100 characters.");
        }

        var description = request.Description?.Trim();
        if (description?.Length > 1000)
        {
            throw new ArgumentException("Description must not exceed 1000 characters.");
        }

        var existing = await _repository.GetPostReportByPostAndReporterAsync(postId, reporterUserId);
        if (existing != null)
        {
            throw new InvalidOperationException("You have already reported this post.");
        }

        var report = new CompanyPostReport
        {
            CompanyPostId = postId,
            ReporterUserId = reporterUserId,
            Reason = reason,
            Description = description,
            Status = Company.Domain.Enums.PostReportStatus.Pending,
            CreatedAt = DateTimeHelper.GetVietnamTime()
        };

        var created = await _repository.CreatePostReportAsync(report);
        created.CompanyPost = post;

        var reportCreatedEvent = new PostReportCreatedNotificationEvent
        {
            EventId = Guid.NewGuid().ToString("N"),
            EventType = "post.report.created",
            Version = 1,
            ActorId = reporterUserId.ToString(),
            ActorType = "USER",
            ObjectId = postId.ToString(),
            Title = "Báo cáo bài đăng mới",
            Content = $"Bài đăng #{postId} có báo cáo mới cần được kiểm duyệt.",
            Type = "COMPANY_REPORT_REVIEW",
            TargetRoles = new[] { "ADMIN", "MODERATOR" },
            CreatedAt = DateTimeHelper.GetVietnamTime()
        };

        try
        {
            await _notificationPublisher.PublishReportCreatedAsync(reportCreatedEvent);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish report notification for post {PostId}", postId);
        }

        return new CompanyPostReportDto
        {
            Id = created.Id,
            CompanyPostId = created.CompanyPostId,
            PostOwnerUserId = post.CompanyId,
            ReporterUserId = created.ReporterUserId,
            Reason = created.Reason,
            Description = created.Description,
            Status = created.Status.ToString(),
            ReviewedByUserId = created.ReviewedByUserId,
            ReviewedAt = created.ReviewedAt,
            ReviewNote = created.ReviewNote,
            CreatedAt = created.CreatedAt,
            UpdatedAt = created.UpdatedAt
        };
    }

    public async Task<PagedResult<CompanyPostReportDto>> GetPostReportsAsync(int page, int pageSize)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 20;
        if (pageSize > 100) pageSize = 100;

        var (items, total) = await _repository.GetPostReportsAsync(page, pageSize);
        return new PagedResult<CompanyPostReportDto>
        {
            Items = items.Select(report => new CompanyPostReportDto
            {
                Id = report.Id,
                CompanyPostId = report.CompanyPostId,
                PostOwnerUserId = report.CompanyPost?.CompanyId ?? 0,
                ReporterUserId = report.ReporterUserId,
                Reason = report.Reason,
                Description = report.Description,
                Status = report.Status.ToString(),
                ReviewedByUserId = report.ReviewedByUserId,
                ReviewedAt = report.ReviewedAt,
                ReviewNote = report.ReviewNote,
                CreatedAt = report.CreatedAt,
                UpdatedAt = report.UpdatedAt
            }).ToList(),
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<CompanyPostReportDto> ReviewPostReportAsync(int reportId, int reviewerUserId, ReviewPostReportRequest request)
    {
        var report = await _repository.GetPostReportByIdAsync(reportId)
            ?? throw new KeyNotFoundException($"Report {reportId} not found");

        if (report.Status != PostReportStatus.Pending)
        {
            throw new InvalidOperationException("This report has already been reviewed.");
        }

        var action = request.Action?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException("Action is required.");
        }

        var now = DateTimeHelper.GetVietnamTime();
        report.ReviewedByUserId = reviewerUserId;
        report.ReviewedAt = now;
        report.ReviewNote = request.ReviewNote?.Trim();
        report.UpdatedAt = now;

        if (action == "approve_violation")
        {
            report.Status = PostReportStatus.Approved;

            if (report.CompanyPost != null && report.CompanyPost.Status == CompanyPost.StatusActive)
            {
                // Soft delete the post
                await _repository.SoftDeletePostAsync(report.CompanyPostId);

                // Notify owner using PostRejectedNotificationEvent structure
                var evt = new PostRejectedNotificationEvent
                {
                    EventId = Guid.NewGuid().ToString("N"),
                    EventType = "post.removed",
                    Version = 1,
                    UserId = report.CompanyPost.CompanyId.ToString(),
                    ActorId = reviewerUserId.ToString(),
                    ActorType = "ADMIN",
                    ObjectId = report.CompanyPostId.ToString(),
                    Title = "Your job post was removed",
                    Content = "Your company job post has been removed due to violation of policies.",
                    Type = "POST_REMOVED",
                    PostType = "Company",
                    CreatedAt = now
                };

                try
                {
                    await _notificationPublisher.PublishPostRejectedNotificationAsync(evt);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to publish post removed notification for post {PostId}", report.CompanyPostId);
                }

                var realtimeEvt = new PostModerationEvent
                {
                    EventId = Guid.NewGuid().ToString("N"),
                    EventType = "post.moderation",
                    Version = 1,
                    PostId = report.CompanyPostId,
                    UserId = report.CompanyPost.CompanyId.ToString(),
                    Status = "REMOVED",
                    Reason = report.ReviewNote ?? "Removed by moderation",
                    PostType = "Company",
                    Title = "Your job post was removed",
                    Content = "Your company job post has been removed due to violation of policies.",
                    ActorId = reviewerUserId.ToString(),
                    ActorType = "ADMIN",
                    CreatedAt = now
                };

                try
                {
                    await _embeddingEventPublisher.PublishCompanyPostChangedAsync(report.CompanyPostId);
                }
                catch { /* swallow */ }
            }
        }
        else if (action == "reject")
        {
            report.Status = PostReportStatus.Rejected;
        }
        else
        {
            throw new ArgumentException("Action must be one of: approve_violation, reject.");
        }

        await _repository.UpdatePostReportAsync(report);

        return new CompanyPostReportDto
        {
            Id = report.Id,
            CompanyPostId = report.CompanyPostId,
            PostOwnerUserId = report.CompanyPost?.CompanyId ?? 0,
            ReporterUserId = report.ReporterUserId,
            Reason = report.Reason,
            Description = report.Description,
            Status = report.Status.ToString(),
            ReviewedByUserId = report.ReviewedByUserId,
            ReviewedAt = report.ReviewedAt,
            ReviewNote = report.ReviewNote,
            CreatedAt = report.CreatedAt,
            UpdatedAt = report.UpdatedAt
        };
    }
}



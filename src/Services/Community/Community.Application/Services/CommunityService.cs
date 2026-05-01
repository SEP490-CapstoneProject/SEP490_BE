using Community.Application.Clients;
using Community.Application.DTOs;
using Community.Application.Helpers;
using Community.Application.Interfaces;
using Community.Application.Models.Events;
using Community.Domain.Entities;
using Community.Domain.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using RecruitmentPlatform.Contracts.Realtime;
using RecruitmentPlatform.AI.Services;

namespace Community.Application.Services;

public class CommunityService : ICommunityService
{
    private const int DeletedPostStatus = 0;
    private const int ActivePostStatus = 1;
    private const int RemovedByModerationStatus = 2;

    private readonly ICommunityRepository _repository;
    private readonly IUserInfoClient _userInfoClient;
    private readonly IPortfolioPreviewClient _portfolioPreviewClient;
    private readonly IMediaUploadClient _mediaUploadClient;
    private readonly ICommunityEventPublisher _eventPublisher;
    private readonly INotificationEventPublisher _notificationPublisher;
    private readonly ModerationService _moderationService;
    private readonly ILogger<CommunityService> _logger;

    public CommunityService(
        ICommunityRepository repository,
        IUserInfoClient userInfoClient,
        IPortfolioPreviewClient portfolioPreviewClient,
        IMediaUploadClient mediaUploadClient,
        ICommunityEventPublisher eventPublisher,
        INotificationEventPublisher notificationPublisher,
        ModerationService moderationService,
        ILogger<CommunityService> logger)
    {
        _repository = repository;
        _userInfoClient = userInfoClient;
        _portfolioPreviewClient = portfolioPreviewClient;
        _mediaUploadClient = mediaUploadClient;
        _eventPublisher = eventPublisher;
        _notificationPublisher = notificationPublisher;
        _moderationService = moderationService;
        _logger = logger;
    }

    // ─── Feed ─────────────────────────────────────────────────────────────────

    public async Task<CursorPagedResult<CommunityPostDto>> GetFeedAsync(int? cursor, int pageSize, int? currentUserId, string? searchQuery)
    {
        if (pageSize < 1) pageSize = 50;
        if (pageSize > 100) pageSize = 100;

        // Fetch one extra to determine hasMore
        var posts = await _repository.GetFeedAsync(cursor, pageSize + 1, searchQuery);
        var hasMore = posts.Count > pageSize;
        if (hasMore) posts = posts.Take(pageSize).ToList();

        if (posts.Count == 0)
            return new CursorPagedResult<CommunityPostDto> { Items = new(), HasMore = false };

        var postIds = posts.Select(p => p.Id).ToList();
        var uniqueUserIds = posts.Select(p => p.UserId).Distinct().ToList();

        // Parallel: counts + authors
        var countsTask = _repository.GetFeedCountsAsync(postIds, currentUserId);
        var authorsTask = _userInfoClient.GetAuthorsBatchAsync(uniqueUserIds);

        await Task.WhenAll(countsTask, authorsTask);
        var counts = countsTask.Result;
        var authors = authorsTask.Result;

        // Portfolio previews in parallel (only for posts with portfolioId)
        var portfolioIds = posts.Where(p => p.PortfolioId.HasValue)
                               .Select(p => p.PortfolioId!.Value)
                               .Distinct()
                               .ToList();

        var previews = new Dictionary<int, PortfolioPreviewDto?>();
        if (portfolioIds.Count > 0)
        {
            var previewTasks = portfolioIds.Select(async pid =>
                (pid, preview: await _portfolioPreviewClient.GetPreviewAsync(pid)));
            var previewResults = await Task.WhenAll(previewTasks);
            foreach (var (pid, preview) in previewResults)
                previews[pid] = preview;
        }

        var items = posts.Select(p => MapToDto(p, authors, counts, previews)).ToList();

        return new CursorPagedResult<CommunityPostDto>
        {
            Items = items,
            NextCursor = hasMore ? posts.Last().Id : null,
            HasMore = hasMore
        };
    }

    public async Task<CommunityPostDto?> GetPostDtoAsync(int postId, int? currentUserId)
    {
        var post = await _repository.GetPostByIdAsync(postId);
        if (post == null) return null;

        var postIds = new List<int> { postId };
        var countsTask = _repository.GetFeedCountsAsync(postIds, currentUserId);
        var authorsTask = _userInfoClient.GetAuthorsBatchAsync(new[] { post.UserId });

        await Task.WhenAll(countsTask, authorsTask);

        var counts = countsTask.Result;
        var authors = authorsTask.Result;
        var previews = new Dictionary<int, PortfolioPreviewDto?>();

        if (post.PortfolioId.HasValue)
            previews[post.PortfolioId.Value] = await _portfolioPreviewClient.GetPreviewAsync(post.PortfolioId.Value);

        return MapToDto(post, authors, counts, previews);
    }

    public async Task<List<CommunityPostDto>> GetPostsByUserIdDtoAsync(int userId, int? currentUserId)
    {
        // Get all posts by user
        var posts = (await _repository.GetPostsByUserIdAsync(userId)).ToList();
        
        if (posts.Count == 0)
            return new List<CommunityPostDto>();

        var postIds = posts.Select(p => p.Id).ToList();
        var uniqueUserIds = new List<int> { userId }; // Only one user in this case

        // Parallel: counts + authors
        var countsTask = _repository.GetFeedCountsAsync(postIds, currentUserId);
        var authorsTask = _userInfoClient.GetAuthorsBatchAsync(uniqueUserIds);

        await Task.WhenAll(countsTask, authorsTask);
        var counts = countsTask.Result;
        var authors = authorsTask.Result;

        // Portfolio previews in parallel (only for posts with portfolioId)
        var portfolioIds = posts.Where(p => p.PortfolioId.HasValue)
                                .Select(p => p.PortfolioId!.Value)
                                .Distinct()
                                .ToList();

        var previews = new Dictionary<int, PortfolioPreviewDto?>();
        if (portfolioIds.Count > 0)
        {
            var previewTasks = portfolioIds.Select(async pid =>
                (pid, preview: await _portfolioPreviewClient.GetPreviewAsync(pid)));
            var previewResults = await Task.WhenAll(previewTasks);
            foreach (var (pid, preview) in previewResults)
                previews[pid] = preview;
        }

        return posts.Select(p => MapToDto(p, authors, counts, previews)).ToList();
    }

    public async Task<PostCommentsResponseDto> GetCommentsResponseAsync(int postId)
    {
        var comments = (await _repository.GetCommentsByPostIdAsync(postId)).ToList();
        if (comments.Count == 0)
            return new PostCommentsResponseDto { PostId = postId, Comments = new() };

        var commentIds = comments.Select(c => c.Id).ToList();
        var repliesTask = _repository.GetRepliesByCommentIdsAsync(commentIds);

        // Collect all unique userIds from comments + replies
        var allUserIds = comments.Select(c => c.UserId).ToList();

        // Need replies to get reply userIds — fetch first
        var replies = (await repliesTask).ToList();
        allUserIds.AddRange(replies.Select(r => r.UserId));
        allUserIds.AddRange(replies.Where(r => r.ReplyToUserId.HasValue).Select(r => r.ReplyToUserId!.Value));

        var authors = await _userInfoClient.GetAuthorsBatchAsync(allUserIds.Distinct());

        var replyLookup = replies.GroupBy(r => r.CommentId).ToDictionary(g => g.Key, g => g.ToList());

        var commentDtos = comments.Select(c =>
        {
            var authorDto = authors.TryGetValue(c.UserId, out var a) ? a : Fallback(c.UserId);
            var commentReplies = replyLookup.TryGetValue(c.Id, out var rList) ? rList : new List<ReplyComment>();

            return new PostCommentDto
            {
                Id = c.Id,
                Author = new CommentUserDto { Id = authorDto.Id, Name = authorDto.Name, Avatar = authorDto.Avatar, Role = authorDto.Role },
                Content = c.Content,
                CreatedAt = c.CreatedAt.ToString("o"),
                Replies = commentReplies.Select(r =>
                {
                    var rAuthor = authors.TryGetValue(r.UserId, out var ra) ? ra : Fallback(r.UserId);
                    CommentUserDto? replyTo = null;
                    if (r.ReplyToUserId.HasValue && authors.TryGetValue(r.ReplyToUserId.Value, out var rta))
                        replyTo = new CommentUserDto { Id = rta.Id, Name = rta.Name, Avatar = rta.Avatar, Role = rta.Role };

                    return new ReplyCommentDto
                    {
                        Id = r.Id,
                        Author = new CommentUserDto { Id = rAuthor.Id, Name = rAuthor.Name, Avatar = rAuthor.Avatar, Role = rAuthor.Role },
                        ReplyToUser = replyTo,
                        Content = r.Content,
                        CreatedAt = r.CreatedAt.ToString("o")
                    };
                }).ToList()
            };
        }).ToList();

        return new PostCommentsResponseDto { PostId = postId, Comments = commentDtos };
    }

    // ─── Post operations ─────────────────────────────────────────────────────

    public async Task<CommunityPost?> GetPostByIdAsync(int id) => await _repository.GetPostByIdAsync(id);
    public async Task<IEnumerable<CommunityPost>> GetAllPostsAsync() => await _repository.GetAllPostsAsync();
    public async Task<OffsetPagedResult<AdminCommunityPostDto>> GetAdminPostsAsync(AdminPostFilter filter, int? currentUserId)
    {
        var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
        var pageSize = filter.PageSize < 1 ? 20 : filter.PageSize;
        if (pageSize > 100)
        {
            pageSize = 100;
        }

        int? status = null;
        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            if (!int.TryParse(filter.Status, out var parsedStatus))
            {
                throw new ArgumentException("Status must be a valid integer.");
            }

            status = parsedStatus;
        }

        var posts = await _repository.GetAdminPostsAsync(status, pageNumber, pageSize + 1);
        var hasMore = posts.Count > pageSize;
        if (hasMore)
        {
            posts = posts.Take(pageSize).ToList();
        }

        if (posts.Count == 0)
        {
            return new OffsetPagedResult<AdminCommunityPostDto>
            {
                Items = new(),
                PageNumber = pageNumber,
                PageSize = pageSize,
                HasMore = false
            };
        }

        var postIds = posts.Select(p => p.Id).ToList();
        var uniqueUserIds = posts.Select(p => p.UserId).Distinct().ToList();

        var countsTask = _repository.GetFeedCountsAsync(postIds, currentUserId);
        var authorsTask = _userInfoClient.GetAuthorsBatchAsync(uniqueUserIds);

        await Task.WhenAll(countsTask, authorsTask);
        var counts = countsTask.Result;
        var authors = authorsTask.Result;

        var portfolioIds = posts.Where(p => p.PortfolioId.HasValue)
            .Select(p => p.PortfolioId!.Value)
            .Distinct()
            .ToList();

        var previews = new Dictionary<int, PortfolioPreviewDto?>();
        if (portfolioIds.Count > 0)
        {
            var previewTasks = portfolioIds.Select(async pid =>
                (pid, preview: await _portfolioPreviewClient.GetPreviewAsync(pid)));
            var previewResults = await Task.WhenAll(previewTasks);
            foreach (var (pid, preview) in previewResults)
            {
                previews[pid] = preview;
            }
        }

        return new OffsetPagedResult<AdminCommunityPostDto>
        {
            Items = posts.Select(p => MapToAdminDto(p, authors, counts, previews)).ToList(),
            PageNumber = pageNumber,
            PageSize = pageSize,
            HasMore = hasMore
        };
    }
    public async Task<IEnumerable<CommunityPost>> GetPostsByUserIdAsync(int userId) => await _repository.GetPostsByUserIdAsync(userId);
    public async Task UpdatePostAsync(CommunityPost post) => await _repository.UpdatePostAsync(post);
    public async Task DeletePostAsync(int id, int actorUserId, string? actorRole)
    {
        var post = await _repository.GetPostByIdAsync(id)
            ?? throw new KeyNotFoundException($"Post {id} not found");

        var wasDeleted = post.Status == DeletedPostStatus;
        var now = DateTimeHelper.GetVietnamTime();
        post.Status = DeletedPostStatus;
        post.UpdatedAt = now;

        await _repository.DeletePostAsync(post);

        if (wasDeleted || post.UserId == actorUserId)
        {
            return;
        }

        var notificationEvt = new PostRemovedByModerationNotificationEvent
        {
            EventId = Guid.NewGuid().ToString("N"),
            EventType = "post.report.removed",
            Version = 1,
            UserId = post.UserId.ToString(),
            ActorId = actorUserId.ToString(),
            ActorType = ResolveActorType(actorRole),
            ObjectId = post.Id.ToString(),
            Title = "Bài đăng đã bị xóa",
            Content = "Bài đăng cộng đồng của bạn đã bị quản trị viên xóa.",
            Type = "COMMUNITY_MODERATION",
            CreatedAt = now
        };

        await _notificationPublisher.PublishPostRemovedByModerationNotificationAsync(notificationEvt);
    }

    public async Task<CommunityPostDto> CreatePostAsync(
        CreatePostRequest request,
        int userId,
        Dictionary<string, IFormFile> fileMap)
    {
        // Run moderation check before creating post
        var moderationResult = _moderationService.CheckPost(request.Description);

        var post = new CommunityPost
        {
            UserId = userId,
            Description = request.Description,
            CoverImageVideo = string.Empty,
            PortfolioId = request.PortfolioId,
            Status = request.Status,
            CreatedAt = DateTimeHelper.GetVietnamTime()
        };

        // Set moderation fields based on check result
        if (moderationResult.Status == "Rejected")
        {
            post.Status = CommunityPost.StatusInactive;
            post.ReviewStatus = CommunityPost.StatusRejected;
            post.ReviewReason = moderationResult.Reason;
            post.ReviewedAt = DateTimeHelper.GetVietnamTime();
        }
        else if (moderationResult.Status == "PendingReview")
        {
            post.ReviewStatus = CommunityPost.StatusPendingReview;
            post.ReviewReason = moderationResult.Reason;
            post.ReviewedAt = DateTimeHelper.GetVietnamTime();
        }
        // else: Approved stays as StatusActive (default)

        var created = await _repository.CreatePostAsync(post);

        // Publish notifications based on moderation result
        if (created.ReviewStatus == CommunityPost.StatusRejected)
        {
            // Auto-rejected - notify user
            var evt = new PostRejectedNotificationEvent
            {
                EventId = Guid.NewGuid().ToString("N"),
                EventType = "post.rejected",
                Version = 1,
                UserId = created.UserId.ToString(),
                ActorId = "SYSTEM",
                ActorType = "SYSTEM",
                ObjectId = created.Id.ToString(),
                Title = "Your post was rejected",
                Content = $"Your community post was automatically rejected. Reason: {moderationResult.Reason}",
                Type = "POST_REJECTED",
                PostType = "Community",
                CreatedAt = DateTimeHelper.GetVietnamTime()
            };

            await _notificationPublisher.PublishPostRejectedNotificationAsync(evt);

            // Also publish realtime event for instant feedback
            var realtimeEvt = new PostModerationEvent
            {
                EventId = Guid.NewGuid().ToString("N"),
                EventType = "post.moderation",
                Version = 1,
                PostId = created.Id,
                UserId = created.UserId.ToString(),
                Status = "REJECTED",
                Reason = moderationResult.Reason,
                PostType = "Community",
                Title = "Bài đăng của bạn đã bị từ chối",
                Content = $"Bài đăng cộng đồng của bạn đã bị từ chối. Lý do: {moderationResult.Reason}",
                ActorId = "SYSTEM",
                ActorType = "SYSTEM",
                CreatedAt = DateTimeHelper.GetVietnamTime()
            };

            await _eventPublisher.PublishPostModerationEventAsync(realtimeEvt);
        }
        else if (created.ReviewStatus == CommunityPost.StatusPendingReview)
        {
            // Needs manual review - notify user
            var evt = new PostPendingReviewNotificationEvent
            {
                EventId = Guid.NewGuid().ToString("N"),
                EventType = "post.pending.review",
                Version = 1,
                UserId = created.UserId.ToString(),
                ActorId = null,
                ActorType = "SYSTEM",
                ObjectId = created.Id.ToString(),
                Title = "Bài đăng của bạn đang được xem xét",
                Content = $"Bài đăng cộng đồng của bạn đang chờ xem xét thủ công. Lý do: {moderationResult.Reason}",
                Type = "POST_PENDING_REVIEW",
                PostType = "Community",
                CreatedAt = DateTimeHelper.GetVietnamTime()
            };

            await _notificationPublisher.PublishPostPendingReviewNotificationAsync(evt);

            // Also publish realtime event
            var realtimeEvt = new PostModerationEvent
            {
                EventId = Guid.NewGuid().ToString("N"),
                EventType = "post.moderation",
                Version = 1,
                PostId = created.Id,
                UserId = created.UserId.ToString(),
                Status = "PENDING_REVIEW",
                Reason = moderationResult.Reason,
                PostType = "Community",
                Title = "Bài đăng của bạn đang được xem xét",
                Content = $"Bài đăng cộng đồng của bạn đang chờ xem xét thủ công. Lý do: {moderationResult.Reason}",
                ActorId = "SYSTEM",
                ActorType = "SYSTEM",
                CreatedAt = DateTimeHelper.GetVietnamTime()
            };

            await _eventPublisher.PublishPostModerationEventAsync(realtimeEvt);
        }
        else
        {
            // Auto-approved - notify user
            var evt = new PostApprovedNotificationEvent
            {
                EventId = Guid.NewGuid().ToString("N"),
                EventType = "post.approved",
                Version = 1,
                UserId = created.UserId.ToString(),
                ActorId = "SYSTEM",
                ActorType = "SYSTEM",
                ObjectId = created.Id.ToString(),
                Title = "Bài đăng của bạn đã được duyệt",
                Content = "Bài đăng cộng đồng của bạn đã được tự động duyệt và hiện đang hiển thị.",
                Type = "POST_APPROVED",
                PostType = "Community",
                ApproverNotes = "Auto-approved by content moderation system",
                CreatedAt = DateTimeHelper.GetVietnamTime()
            };

            await _notificationPublisher.PublishPostApprovedNotificationAsync(evt);

            // Also publish realtime event
            var realtimeEvt = new PostModerationEvent
            {
                EventId = Guid.NewGuid().ToString("N"),
                EventType = "post.moderation",
                Version = 1,
                PostId = created.Id,
                UserId = created.UserId.ToString(),
                Status = "APPROVED",
                Reason = "Auto-approved by content moderation system",
                PostType = "Community",
                Title = "Bài đăng của bạn đã được duyệt",
                Content = "Bài đăng cộng đồng của bạn đã được tự động duyệt và hiện đang hiển thị.",
                ActorId = "SYSTEM",
                ActorType = "SYSTEM",
                CreatedAt = DateTimeHelper.GetVietnamTime()
            };

            await _eventPublisher.PublishPostModerationEventAsync(realtimeEvt);
        }

        if (fileMap.Count == 0)
        {
            // Return DTO immediately if no files
            var dto = await GetPostDtoAsync(created.Id, userId);
            return dto!;
        }

        // Upload cover image if specified
        if (!string.IsNullOrWhiteSpace(request.CoverImageKey)
            && fileMap.TryGetValue(request.CoverImageKey, out var coverFile))
        {
            var result = await _mediaUploadClient.UploadAsync(coverFile, "community/posts");
            if (result != null)
            {
                created.CoverImageVideo = result.Url;
                await _repository.UpdatePostAsync(created);
            }
            else
            {
                _logger.LogWarning("Cover image upload failed for post {PostId}", created.Id);
            }
        }

        // Upload remaining files as post media
        var coverKey = request.CoverImageKey ?? string.Empty;
        foreach (var (filename, file) in fileMap)
        {
            if (!string.IsNullOrWhiteSpace(coverKey)
                && string.Equals(filename, coverKey, StringComparison.OrdinalIgnoreCase))
                continue;

            var result = await _mediaUploadClient.UploadAsync(file, "community/posts");
            if (result != null)
            {
                var mediaType = file.ContentType.StartsWith("video/") ? "video" : "image";
                await _repository.AddMediaAsync(new CommunityPostMedia
                {
                    CommunityPostId = created.Id,
                    Address = result.Url,
                    Type = mediaType,
                    Name = result.PublicId ?? filename
                });
            }
            else
            {
                _logger.LogWarning("Media upload failed for {Filename} on post {PostId}", filename, created.Id);
            }
        }

        // Return DTO with all enriched data
        var postDto = await GetPostDtoAsync(created.Id, userId);
        return postDto!;
    }

    public async Task<CommunityPostReportDto> ReportPostAsync(int postId, int reporterUserId, CreatePostReportRequest request)
    {
        var post = await _repository.GetPostByIdAsync(postId)
            ?? throw new KeyNotFoundException($"Post {postId} not found");

        if (post.UserId == reporterUserId)
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

        var report = new CommunityPostReport
        {
            CommunityPostId = postId,
            ReporterUserId = reporterUserId,
            Reason = reason,
            Description = description,
            Status = PostReportStatus.Pending,
            CreatedAt = DateTimeHelper.GetVietnamTime()
        };

        var created = await _repository.CreatePostReportAsync(report);
        created.CommunityPost = post;

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
            Type = "COMMUNITY_REPORT_REVIEW",
            CreatedAt = DateTimeHelper.GetVietnamTime(),
            PostId = postId,
            ReportId = created.Id,
            ReporterUserId = reporterUserId,
            Reason = reason,
            TargetRoles = ["ADMIN", "MODERATOR"]
        };

        await _notificationPublisher.PublishPostReportCreatedNotificationAsync(reportCreatedEvent);

        return MapToReportDto(created);
    }

    public async Task<List<CommunityPostReportDto>> GetPostReportsAsync(AdminPostReportFilter filter)
    {
        var pageNumber = filter.PageNumber < 1 ? 1 : filter.PageNumber;
        var pageSize = filter.PageSize < 1 ? 20 : filter.PageSize;
        if (pageSize > 100)
        {
            pageSize = 100;
        }

        int? status = null;
        if (!string.IsNullOrWhiteSpace(filter.Status))
        {
            if (!Enum.TryParse<PostReportStatus>(filter.Status, true, out var parsedStatus))
            {
                throw new ArgumentException("Status must be one of: Pending, Approved, Rejected.");
            }

            status = (int)parsedStatus;
        }

        var reports = await _repository.GetPostReportsAsync(
            filter.PostId,
            filter.ReporterUserId,
            status,
            pageNumber,
            pageSize);

        return reports.Select(MapToReportDto).ToList();
    }

    public async Task<CommunityPostReportDto> ReviewPostReportAsync(int reportId, int reviewerUserId, ReviewPostReportRequest request)
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

            if (report.CommunityPost.Status == ActivePostStatus)
            {
                report.CommunityPost.Status = RemovedByModerationStatus;
                report.CommunityPost.UpdatedAt = now;
                await _repository.UpdatePostAsync(report.CommunityPost);
            }

            var notificationEvt = new PostRemovedByModerationNotificationEvent
            {
                EventId = Guid.NewGuid().ToString("N"),
                EventType = "post.report.removed",
                Version = 1,
                UserId = report.CommunityPost.UserId.ToString(),
                ActorId = reviewerUserId.ToString(),
                ActorType = "ADMIN",
                ObjectId = report.CommunityPostId.ToString(),
                Title = "Bài đăng bị gỡ do vi phạm",
                Content = "Bài đăng cộng đồng của bạn đã bị gỡ vì vi phạm tiêu chuẩn cộng đồng.",
                Type = "COMMUNITY_MODERATION",
                CreatedAt = now
            };

            await _notificationPublisher.PublishPostRemovedByModerationNotificationAsync(notificationEvt);
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

        return MapToReportDto(report);
    }

    // ─── Ownership checks ─────────────────────────────────────────────────────

    public async Task<int?> GetCommentOwnerAsync(int commentId) => await _repository.GetCommentOwnerAsync(commentId);
    public async Task<int?> GetReplyOwnerAsync(int replyId) => await _repository.GetReplyOwnerAsync(replyId);

    // ─── Interaction operations ───────────────────────────────────────────────

    public async Task<bool> SavePostAsync(int postId, int userId) => await _repository.SavePostAsync(postId, userId);
    public async Task<bool> UnsavePostAsync(int postId, int userId) => await _repository.UnsavePostAsync(postId, userId);
    
    public async Task<bool> FavoritePostAsync(int postId, int userId)
    {
        var result = await _repository.FavoritePostAsync(postId, userId);
        if (result)
        {
            // Get post details to check owner
            var post = await _repository.GetPostByIdAsync(postId);
            if (post != null && post.UserId != userId) // Don't notify self-favorite
            {
                // Get actor (favoriter) info for notification
                var actors = await _userInfoClient.GetAuthorsBatchAsync(new[] { userId });
                var actorInfo = actors.FirstOrDefault().Value; // Get AuthorDto from KeyValuePair
                
                var notificationEvt = new PostFavoriteNotificationEvent
                {
                    EventId = Guid.NewGuid().ToString("N"),
                    EventType = "post.favorite",
                    Version = 1,
                    UserId = post.UserId.ToString(), // Post owner receives notification
                    ActorId = userId.ToString(),     // Person who favorited
                    ActorType = "USER",
                    ObjectId = postId.ToString(),
                    Title = "Lượt thích mới",
                    Content = $"{actorInfo?.Name ?? "Ai đó"} đã thích bài viết của bạn",
                    Type = "POST_FAVORITE",
                    CreatedAt = DateTimeHelper.GetVietnamTime()
                };

                await _notificationPublisher.PublishPostFavoriteNotificationAsync(notificationEvt);
            }

            // Get new favorite count for realtime update
            var favoriteCount = await _repository.GetPostFavoriteCountAsync(postId);
            
            // Get actor info for realtime event (who did the favorite)
            var actorData = await _userInfoClient.GetAuthorsBatchAsync(new[] { userId });
            var favoriterInfo = actorData.FirstOrDefault().Value;

            var realtimeEvt = new PostFavoriteChangedEvent
            {
                EventId = Guid.NewGuid().ToString("N"),
                EventType = "post.favorite.changed",
                Version = 1,
                PostId = postId,
                UserId = userId,
                Action = "FAVORITE",
                NewFavoriteCount = favoriteCount,
                Actor = favoriterInfo != null ? new NotificationActorDto
                {
                    Id = userId,
                    Name = favoriterInfo.Name ?? "Unknown",
                    Avatar = favoriterInfo.Avatar ?? string.Empty,
                    Role = "USER"
                } : null,
                CreatedAt = DateTimeHelper.GetVietnamTime()
            };

            await _eventPublisher.PublishPostFavoriteChangedAsync(realtimeEvt);
        }
        return result;
    }
    
    public async Task<bool> UnfavoritePostAsync(int postId, int userId)
    {
        var result = await _repository.UnfavoritePostAsync(postId, userId);
        if (result)
        {
            // Get new favorite count
            var favoriteCount = await _repository.GetPostFavoriteCountAsync(postId);

            var evt = new PostFavoriteChangedEvent
            {
                EventId = Guid.NewGuid().ToString("N"),
                EventType = "post.favorite.changed",
                Version = 1,
                PostId = postId,
                UserId = userId,
                Action = "UNFAVORITE",
                NewFavoriteCount = favoriteCount,
                CreatedAt = DateTimeHelper.GetVietnamTime()
            };

            await _eventPublisher.PublishPostFavoriteChangedAsync(evt);
        }
        return result;
    }
    
    public async Task<List<CommunityPostDto>> GetSavedPostsAsync(int userId)
    {
        var posts = (await _repository.GetSavedPostsByUserAsync(userId)).ToList();
        
        if (posts.Count == 0)
            return new List<CommunityPostDto>();

        var postIds = posts.Select(p => p.Id).ToList();
        var uniqueUserIds = posts.Select(p => p.UserId).Distinct().ToList();

        // Parallel: counts + authors
        var countsTask = _repository.GetFeedCountsAsync(postIds, userId);
        var authorsTask = _userInfoClient.GetAuthorsBatchAsync(uniqueUserIds);

        await Task.WhenAll(countsTask, authorsTask);
        var counts = countsTask.Result;
        var authors = authorsTask.Result;

        // Portfolio previews
        var portfolioIds = posts.Where(p => p.PortfolioId.HasValue)
                                .Select(p => p.PortfolioId!.Value)
                                .Distinct()
                                .ToList();

        var previews = new Dictionary<int, PortfolioPreviewDto?>();
        if (portfolioIds.Count > 0)
        {
            var previewTasks = portfolioIds.Select(async pid =>
                (pid, preview: await _portfolioPreviewClient.GetPreviewAsync(pid)));
            var previewResults = await Task.WhenAll(previewTasks);
            foreach (var (pid, preview) in previewResults)
                previews[pid] = preview;
        }

        return posts.Select(p => MapToDto(p, authors, counts, previews)).ToList();
    }
    
    public async Task<List<CommunityPostDto>> GetFavoritedPostsAsync(int userId)
    {
        var posts = (await _repository.GetFavoritedPostsByUserAsync(userId)).ToList();
        
        if (posts.Count == 0)
            return new List<CommunityPostDto>();

        var postIds = posts.Select(p => p.Id).ToList();
        var uniqueUserIds = posts.Select(p => p.UserId).Distinct().ToList();

        // Parallel: counts + authors
        var countsTask = _repository.GetFeedCountsAsync(postIds, userId);
        var authorsTask = _userInfoClient.GetAuthorsBatchAsync(uniqueUserIds);

        await Task.WhenAll(countsTask, authorsTask);
        var counts = countsTask.Result;
        var authors = authorsTask.Result;

        // Portfolio previews
        var portfolioIds = posts.Where(p => p.PortfolioId.HasValue)
                                .Select(p => p.PortfolioId!.Value)
                                .Distinct()
                                .ToList();

        var previews = new Dictionary<int, PortfolioPreviewDto?>();
        if (portfolioIds.Count > 0)
        {
            var previewTasks = portfolioIds.Select(async pid =>
                (pid, preview: await _portfolioPreviewClient.GetPreviewAsync(pid)));
            var previewResults = await Task.WhenAll(previewTasks);
            foreach (var (pid, preview) in previewResults)
                previews[pid] = preview;
        }

        return posts.Select(p => MapToDto(p, authors, counts, previews)).ToList();
    }

    // ─── Comment operations ───────────────────────────────────────────────────

    public async Task<PostCommentDto> AddCommentAsync(int postId, int userId, string content)
    {
        var comment = new Comment 
        { 
            CommunityPostId = postId, 
            UserId = userId, 
            Content = content,
            CreatedAt = DateTimeHelper.GetVietnamTime()
        };
        var created = await _repository.AddCommentAsync(comment);

        // Fetch author data for DTO and event
        var authors = await _userInfoClient.GetAuthorsBatchAsync(new[] { userId });
        var authorDto = authors.TryGetValue(userId, out var a) ? a : Fallback(userId);

        var post = await _repository.GetPostByIdAsync(postId);
        if (post is not null)
        {
            var recipientUserId = post.UserId;
            if (recipientUserId == userId)
            {
                return new PostCommentDto
                {
                    Id = created.Id,
                    Author = new CommentUserDto { Id = authorDto.Id, Name = authorDto.Name, Avatar = authorDto.Avatar, Role = authorDto.Role },
                    Content = created.Content,
                    CreatedAt = created.CreatedAt.ToString("o"),
                    Replies = new List<ReplyCommentDto>()
                };
            }

            var evt = new CommentCreatedEvent
            {
                EventId = Guid.NewGuid().ToString("N"),
                EventType = "post.comment.created",
                Version = 1,
                PostId = postId,
                CommentId = created.Id,
                UserId = recipientUserId.ToString(),
                ActorId = userId.ToString(),
                ActorType = "USER",
                ObjectId = postId.ToString(),
                Title = "Bình luận mới",
                Type = "COMMUNITY",
                Content = created.Content,
                CreatedAt = created.CreatedAt,
                Author = new RealtimeUserDto
                {
                    Id = authorDto.Id.ToString(),
                    Name = authorDto.Name,
                    Avatar = authorDto.Avatar,
                    Role = authorDto.Role
                }
            };

            await _eventPublisher.PublishCommentCreatedAsync(evt);
        }

        return new PostCommentDto
        {
            Id = created.Id,
            Author = new CommentUserDto { Id = authorDto.Id, Name = authorDto.Name, Avatar = authorDto.Avatar, Role = authorDto.Role },
            Content = created.Content,
            CreatedAt = created.CreatedAt.ToString("o"),
            Replies = new List<ReplyCommentDto>()
        };
    }

    public async Task<IEnumerable<Comment>> GetCommentsAsync(int postId) => await _repository.GetCommentsByPostIdAsync(postId);
    public async Task DeleteCommentAsync(int commentId) => await _repository.DeleteCommentAsync(commentId);

    // ─── Reply operations ─────────────────────────────────────────────────────

    public async Task<ReplyCommentDto> AddReplyAsync(int commentId, int userId, int? replyToUserId, string content)
    {
        var reply = new ReplyComment 
        { 
            CommentId = commentId, 
            UserId = userId, 
            ReplyToUserId = replyToUserId, 
            Content = content,
            CreatedAt = DateTimeHelper.GetVietnamTime()
        };
        var created = await _repository.AddReplyAsync(reply);

        // Fetch author data for DTO and event (include replyToUser if present)
        var userIds = replyToUserId.HasValue 
            ? new[] { userId, replyToUserId.Value } 
            : new[] { userId };
        var authors = await _userInfoClient.GetAuthorsBatchAsync(userIds);
        
        var authorDto = authors.TryGetValue(userId, out var a) ? a : Fallback(userId);
        CommentUserDto? replyToUserDto = null;
        RealtimeUserDto? replyToUserEventDto = null;
        if (replyToUserId.HasValue && authors.TryGetValue(replyToUserId.Value, out var rta))
        {
            replyToUserDto = new CommentUserDto { Id = rta.Id, Name = rta.Name, Avatar = rta.Avatar, Role = rta.Role };
            replyToUserEventDto = new RealtimeUserDto 
            { 
                Id = rta.Id.ToString(), 
                Name = rta.Name, 
                Avatar = rta.Avatar, 
                Role = rta.Role 
            };
        }

        var comment = await _repository.GetCommentByIdAsync(commentId);
        if (comment is not null)
        {
            var recipientUserId = replyToUserId ?? comment.UserId;
            if (recipientUserId != userId)
            {
                var evt = new ReplyCreatedEvent
                {
                    EventId = Guid.NewGuid().ToString("N"),
                    EventType = "post.reply.created",
                    Version = 1,
                    PostId = comment.CommunityPostId,
                    CommentId = commentId,
                    ParentCommentId = commentId,
                    UserId = recipientUserId.ToString(),
                    ActorId = userId.ToString(),
                    ActorType = "USER",
                    ObjectId = comment.CommunityPostId.ToString(),
                    Title = "Trả lời bình luận",
                    Type = "COMMUNITY",
                    ReplyToUserId = replyToUserId,
                    Content = created.Content,
                    CreatedAt = created.CreatedAt,
                    Author = new RealtimeUserDto
                    {
                        Id = authorDto.Id.ToString(),
                        Name = authorDto.Name,
                        Avatar = authorDto.Avatar,
                        Role = authorDto.Role
                    },
                    ReplyToUser = replyToUserEventDto
                };

                await _eventPublisher.PublishReplyCreatedAsync(evt);
            }
        }

        return new ReplyCommentDto
        {
            Id = created.Id,
            Author = new CommentUserDto { Id = authorDto.Id, Name = authorDto.Name, Avatar = authorDto.Avatar, Role = authorDto.Role },
            ReplyToUser = replyToUserDto,
            Content = created.Content,
            CreatedAt = created.CreatedAt.ToString("o")
        };
    }

    public async Task<IEnumerable<ReplyComment>> GetRepliesAsync(int commentId) => await _repository.GetRepliesByCommentIdAsync(commentId);
    public async Task DeleteReplyAsync(int replyId) => await _repository.DeleteReplyAsync(replyId);

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static CommunityPostDto MapToDto(
        CommunityPost p,
        Dictionary<int, AuthorDto> authors,
        FeedCountsResult counts,
        Dictionary<int, PortfolioPreviewDto?> previews)
    {
        authors.TryGetValue(p.UserId, out var author);
        return new CommunityPostDto
        {
            Id = p.Id,
            Author = author ?? Fallback(p.UserId),
            Description = string.IsNullOrEmpty(p.Description) ? null : p.Description,
            CoverImageUrl = string.IsNullOrEmpty(p.CoverImageVideo) ? null : p.CoverImageVideo,
            Media = p.Media.Select(m => m.Address).ToList(),
            PortfolioId = p.PortfolioId,
            PortfolioPreview = p.PortfolioId.HasValue && previews.TryGetValue(p.PortfolioId.Value, out var pv) ? pv : null,
            FavoriteCount = p.FavoriteCount,
            CommentCount = counts.CommentCounts.TryGetValue(p.Id, out var cc) ? cc : 0,
            IsFavorited = counts.FavoritedPostIds.Contains(p.Id),
            IsSaved = counts.SavedPostIds.Contains(p.Id),
            CreatedAt = p.CreatedAt.ToString("o"),
            ReviewStatus = p.ReviewStatus,
            ReviewReason = p.ReviewReason
        };
    }

    private static AdminCommunityPostDto MapToAdminDto(
        CommunityPost p,
        Dictionary<int, AuthorDto> authors,
        FeedCountsResult counts,
        Dictionary<int, PortfolioPreviewDto?> previews)
    {
        var dto = MapToDto(p, authors, counts, previews);
        return new AdminCommunityPostDto
        {
            Id = dto.Id,
            Author = dto.Author,
            Description = dto.Description,
            CoverImageUrl = dto.CoverImageUrl,
            Media = dto.Media,
            PortfolioId = dto.PortfolioId,
            PortfolioPreview = dto.PortfolioPreview,
            FavoriteCount = dto.FavoriteCount,
            CommentCount = dto.CommentCount,
            IsFavorited = dto.IsFavorited,
            IsSaved = dto.IsSaved,
            CreatedAt = dto.CreatedAt,
            ReviewStatus = dto.ReviewStatus,
            ReviewReason = dto.ReviewReason,
            Status = p.Status
        };
    }

    private static CommunityPostReportDto MapToReportDto(CommunityPostReport report)
    {
        return new CommunityPostReportDto
        {
            Id = report.Id,
            CommunityPostId = report.CommunityPostId,
            PostOwnerUserId = report.CommunityPost.UserId,
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

    private static AuthorDto Fallback(int userId) => new() { Id = userId, Name = "Unknown", Avatar = string.Empty, Role = "USER" };

    private static string ResolveActorType(string? role)
    {
        if (string.Equals(role, "ADMIN", StringComparison.OrdinalIgnoreCase))
        {
            return "ADMIN";
        }

        if (string.Equals(role, "MODERATOR", StringComparison.OrdinalIgnoreCase))
        {
            return "MODERATOR";
        }

        if (string.Equals(role, "COMPANY", StringComparison.OrdinalIgnoreCase))
        {
            return "COMPANY";
        }

        return "USER";
    }

    // ─── Admin moderation ─────────────────────────────────────────────────────

    public async Task<OffsetPagedResult<AdminCommunityPostDto>> GetPendingPostsAsync(int skip, int take)
    {
        if (take < 1) take = 20;
        if (take > 100) take = 100;

        var posts = await _repository.GetAdminPostsAsync(CommunityPost.StatusPendingReview, skip / take + 1, take + 1);
        var hasMore = posts.Count > take;
        if (hasMore) posts = posts.Take(take).ToList();

        if (posts.Count == 0)
        {
            return new OffsetPagedResult<AdminCommunityPostDto>
            {
                Items = new(),
                PageNumber = skip / take + 1,
                PageSize = take,
                HasMore = false
            };
        }

        var postIds = posts.Select(p => p.Id).ToList();
        var uniqueUserIds = posts.Select(p => p.UserId).Distinct().ToList();

        var countsTask = _repository.GetFeedCountsAsync(postIds, null);
        var authorsTask = _userInfoClient.GetAuthorsBatchAsync(uniqueUserIds);

        await Task.WhenAll(countsTask, authorsTask);
        var counts = countsTask.Result;
        var authors = authorsTask.Result;

        var portfolioIds = posts.Where(p => p.PortfolioId.HasValue)
            .Select(p => p.PortfolioId!.Value)
            .Distinct()
            .ToList();

        var previews = new Dictionary<int, PortfolioPreviewDto?>();
        if (portfolioIds.Count > 0)
        {
            var previewTasks = portfolioIds.Select(async pid =>
                (pid, preview: await _portfolioPreviewClient.GetPreviewAsync(pid)));
            var previewResults = await Task.WhenAll(previewTasks);
            foreach (var (pid, preview) in previewResults)
                previews[pid] = preview;
        }

        return new OffsetPagedResult<AdminCommunityPostDto>
        {
            Items = posts.Select(p => MapToAdminDto(p, authors, counts, previews)).ToList(),
            PageNumber = skip / take + 1,
            PageSize = take,
            HasMore = hasMore
        };
    }

    public async Task<CommunityPost> ApprovePostAsync(int postId, string? notes)
    {
        var post = await _repository.GetPostByIdAsync(postId);
        if (post == null)
            throw new KeyNotFoundException($"Post not found");

        post.Status = CommunityPost.StatusActive;
        post.ReviewStatus = CommunityPost.StatusActive;
        post.ReviewReason = notes;
        post.ReviewedAt = DateTimeHelper.GetVietnamTime();

        await _repository.UpdatePostAsync(post);

        // Publish approval notification
        var evt = new PostApprovedNotificationEvent
        {
            EventId = Guid.NewGuid().ToString("N"),
            EventType = "post.approved",
            Version = 1,
            UserId = post.UserId.ToString(),
            ActorId = "ADMIN",
            ActorType = "ADMIN",
            ObjectId = post.Id.ToString(),
            Title = "Your post has been approved",
            Content = "Your community post has been approved and is now live.",
            Type = "POST_APPROVED",
            PostType = "Community",
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
            PostId = post.Id,
            UserId = post.UserId.ToString(),
            Status = "APPROVED",
            Reason = notes ?? "Post approved",
            PostType = "Community",
            Title = "Your post has been approved",
            Content = "Your community post has been approved and is now live.",
            CreatedAt = DateTimeHelper.GetVietnamTime()
        };

        await _eventPublisher.PublishPostModerationEventAsync(realtimeEvt);

        return post;
    }

    public async Task<CommunityPost> RejectPostAsync(int postId, string reason)
    {
        var post = await _repository.GetPostByIdAsync(postId);
        if (post == null)
            throw new KeyNotFoundException($"Post not found");

        post.Status = CommunityPost.StatusInactive;
        post.ReviewStatus = CommunityPost.StatusRejected;
        post.ReviewReason = reason;
        post.ReviewedAt = DateTimeHelper.GetVietnamTime();

        await _repository.UpdatePostAsync(post);

        // Publish rejection notification
        var evt = new PostRejectedNotificationEvent
        {
            EventId = Guid.NewGuid().ToString("N"),
            EventType = "post.rejected",
            Version = 1,
            UserId = post.UserId.ToString(),
            ActorId = "ADMIN",
            ActorType = "ADMIN",
            ObjectId = post.Id.ToString(),
            Title = "Your post was rejected",
            Content = $"Your community post was rejected. Reason: {reason}",
            Type = "POST_REJECTED",
            PostType = "Community",
            CreatedAt = DateTimeHelper.GetVietnamTime()
        };

        await _notificationPublisher.PublishPostRejectedNotificationAsync(evt);

        // Publish realtime event
        var realtimeEvt = new PostModerationEvent
        {
            EventId = Guid.NewGuid().ToString("N"),
            EventType = "post.moderation",
            Version = 1,
            PostId = post.Id,
            UserId = post.UserId.ToString(),
            Status = "REJECTED",
            Reason = reason,
            PostType = "Community",
            Title = "Your post was rejected",
            Content = $"Your community post was rejected. Reason: {reason}",
            CreatedAt = DateTimeHelper.GetVietnamTime()
        };

        await _eventPublisher.PublishPostModerationEventAsync(realtimeEvt);

        return post;
    }
}


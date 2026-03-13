using Community.Application.Clients;
using Community.Application.DTOs;
using Community.Application.Interfaces;
using Community.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Community.Application.Services;

public class CommunityService : ICommunityService
{
    private readonly ICommunityRepository _repository;
    private readonly IUserInfoClient _userInfoClient;
    private readonly IPortfolioPreviewClient _portfolioPreviewClient;
    private readonly IMediaUploadClient _mediaUploadClient;
    private readonly ILogger<CommunityService> _logger;

    public CommunityService(
        ICommunityRepository repository,
        IUserInfoClient userInfoClient,
        IPortfolioPreviewClient portfolioPreviewClient,
        IMediaUploadClient mediaUploadClient,
        ILogger<CommunityService> logger)
    {
        _repository = repository;
        _userInfoClient = userInfoClient;
        _portfolioPreviewClient = portfolioPreviewClient;
        _mediaUploadClient = mediaUploadClient;
        _logger = logger;
    }

    // ─── Feed ─────────────────────────────────────────────────────────────────

    public async Task<CursorPagedResult<CommunityPostDto>> GetFeedAsync(int? cursor, int pageSize, int? currentUserId)
    {
        if (pageSize < 1) pageSize = 50;
        if (pageSize > 100) pageSize = 100;

        // Fetch one extra to determine hasMore
        var posts = await _repository.GetFeedAsync(cursor, pageSize + 1);
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
                Author = new CommentUserDto { Id = authorDto.Id, Name = authorDto.Name, Avatar = authorDto.Avatar },
                Content = c.Content,
                CreatedAt = c.CreatedAt.ToString("o"),
                Replies = commentReplies.Select(r =>
                {
                    var rAuthor = authors.TryGetValue(r.UserId, out var ra) ? ra : Fallback(r.UserId);
                    CommentUserDto? replyTo = null;
                    if (r.ReplyToUserId.HasValue && authors.TryGetValue(r.ReplyToUserId.Value, out var rta))
                        replyTo = new CommentUserDto { Id = rta.Id, Name = rta.Name, Avatar = rta.Avatar };

                    return new ReplyCommentDto
                    {
                        Id = r.Id,
                        Author = new CommentUserDto { Id = rAuthor.Id, Name = rAuthor.Name, Avatar = rAuthor.Avatar },
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
    public async Task<IEnumerable<CommunityPost>> GetPostsByUserIdAsync(int userId) => await _repository.GetPostsByUserIdAsync(userId);
    public async Task UpdatePostAsync(CommunityPost post) => await _repository.UpdatePostAsync(post);
    public async Task DeletePostAsync(int id) => await _repository.DeletePostAsync(id);

    public async Task<CommunityPost> CreatePostAsync(
        CreatePostRequest request,
        int userId,
        Dictionary<string, IFormFile> fileMap)
    {
        var post = new CommunityPost
        {
            UserId = userId,
            Description = request.Description,
            CoverImageVideo = string.Empty,
            PortfolioId = request.PortfolioId,
            Status = request.Status
        };

        var created = await _repository.CreatePostAsync(post);

        if (fileMap.Count == 0) return created;

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

        return created;
    }

    // ─── Ownership checks ─────────────────────────────────────────────────────

    public async Task<int?> GetCommentOwnerAsync(int commentId) => await _repository.GetCommentOwnerAsync(commentId);
    public async Task<int?> GetReplyOwnerAsync(int replyId) => await _repository.GetReplyOwnerAsync(replyId);

    // ─── Interaction operations ───────────────────────────────────────────────

    public async Task<bool> SavePostAsync(int postId, int userId) => await _repository.SavePostAsync(postId, userId);
    public async Task<bool> UnsavePostAsync(int postId, int userId) => await _repository.UnsavePostAsync(postId, userId);
    public async Task<bool> FavoritePostAsync(int postId, int userId) => await _repository.FavoritePostAsync(postId, userId);
    public async Task<bool> UnfavoritePostAsync(int postId, int userId) => await _repository.UnfavoritePostAsync(postId, userId);
    public async Task<IEnumerable<CommunityPost>> GetSavedPostsAsync(int userId) => await _repository.GetSavedPostsByUserAsync(userId);
    public async Task<IEnumerable<CommunityPost>> GetFavoritedPostsAsync(int userId) => await _repository.GetFavoritedPostsByUserAsync(userId);

    // ─── Comment operations ───────────────────────────────────────────────────

    public async Task<Comment> AddCommentAsync(int postId, int userId, string content)
    {
        var comment = new Comment { CommunityPostId = postId, UserId = userId, Content = content };
        return await _repository.AddCommentAsync(comment);
    }

    public async Task<IEnumerable<Comment>> GetCommentsAsync(int postId) => await _repository.GetCommentsByPostIdAsync(postId);
    public async Task DeleteCommentAsync(int commentId) => await _repository.DeleteCommentAsync(commentId);

    // ─── Reply operations ─────────────────────────────────────────────────────

    public async Task<ReplyComment> AddReplyAsync(int commentId, int userId, int? replyToUserId, string content)
    {
        var reply = new ReplyComment { CommentId = commentId, UserId = userId, ReplyToUserId = replyToUserId, Content = content };
        return await _repository.AddReplyAsync(reply);
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
            CreatedAt = p.CreatedAt.ToString("o")
        };
    }

    private static AuthorDto Fallback(int userId) => new() { Id = userId, Name = "Unknown", Avatar = string.Empty, Role = "USER" };
}


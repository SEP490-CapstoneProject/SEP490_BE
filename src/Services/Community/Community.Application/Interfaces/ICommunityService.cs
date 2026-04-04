using Community.Application.DTOs;
using Community.Domain.Entities;
using Microsoft.AspNetCore.Http;

namespace Community.Application.Interfaces;

public interface ICommunityService
{
    // Feed (with DTOs)
    Task<CursorPagedResult<CommunityPostDto>> GetFeedAsync(int? cursor, int pageSize, int? currentUserId);
    Task<CommunityPostDto?> GetPostDtoAsync(int postId, int? currentUserId);
    Task<PostCommentsResponseDto> GetCommentsResponseAsync(int postId);
    Task<List<CommunityPostDto>> GetPostsByUserIdDtoAsync(int userId, int? currentUserId);

    // Post operations
    Task<CommunityPost?> GetPostByIdAsync(int id);
    Task<IEnumerable<CommunityPost>> GetAllPostsAsync();
    Task<IEnumerable<CommunityPost>> GetPostsByUserIdAsync(int userId);
    Task<CommunityPostDto> CreatePostAsync(CreatePostRequest request, int userId, Dictionary<string, IFormFile> fileMap);
    Task UpdatePostAsync(CommunityPost post);
    Task DeletePostAsync(int id);

    // Ownership checks
    Task<int?> GetCommentOwnerAsync(int commentId);
    Task<int?> GetReplyOwnerAsync(int replyId);

    // Interaction operations
    Task<bool> SavePostAsync(int postId, int userId);
    Task<bool> UnsavePostAsync(int postId, int userId);
    Task<bool> FavoritePostAsync(int postId, int userId);
    Task<bool> UnfavoritePostAsync(int postId, int userId);
    Task<List<CommunityPostDto>> GetSavedPostsAsync(int userId);
    Task<List<CommunityPostDto>> GetFavoritedPostsAsync(int userId);

    // Comment operations
    Task<PostCommentDto> AddCommentAsync(int postId, int userId, string content);
    Task<IEnumerable<Comment>> GetCommentsAsync(int postId);
    Task DeleteCommentAsync(int commentId);

    // Reply operations
    Task<ReplyCommentDto> AddReplyAsync(int commentId, int userId, int? replyToUserId, string content);
    Task<IEnumerable<ReplyComment>> GetRepliesAsync(int commentId);
    Task DeleteReplyAsync(int replyId);
}


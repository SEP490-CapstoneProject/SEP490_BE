using Community.Application.DTOs;
using Community.Domain.Entities;

namespace Community.Application.Interfaces;

public interface ICommunityRepository
{
    // Feed queries (N+1-free, cursor-based)
    Task<List<CommunityPost>> GetFeedAsync(int? cursor, int pageSize);
    Task<FeedCountsResult> GetFeedCountsAsync(List<int> postIds, int? currentUserId);

    // CommunityPost operations
    Task<CommunityPost?> GetPostByIdAsync(int id);
    Task<IEnumerable<CommunityPost>> GetAllPostsAsync();
    Task<IEnumerable<CommunityPost>> GetPostsByUserIdAsync(int userId);
    Task<CommunityPost> CreatePostAsync(CommunityPost post);
    Task UpdatePostAsync(CommunityPost post);
    Task DeletePostAsync(int id);
    
    // Save/Favorite operations
    Task<bool> SavePostAsync(int postId, int userId);
    Task<bool> UnsavePostAsync(int postId, int userId);
    Task<bool> FavoritePostAsync(int postId, int userId);
    Task<bool> UnfavoritePostAsync(int postId, int userId);
    Task<IEnumerable<CommunityPost>> GetSavedPostsByUserAsync(int userId);
    Task<IEnumerable<CommunityPost>> GetFavoritedPostsByUserAsync(int userId);
    
    // Comment operations
    Task<Comment> AddCommentAsync(Comment comment);
    Task<IEnumerable<Comment>> GetCommentsByPostIdAsync(int postId);
    Task<IEnumerable<ReplyComment>> GetRepliesByCommentIdsAsync(List<int> commentIds);
    Task<Comment?> GetCommentByIdAsync(int commentId);
    Task<int?> GetCommentOwnerAsync(int commentId);
    Task DeleteCommentAsync(int id);
    
    // Reply operations
    Task<ReplyComment> AddReplyAsync(ReplyComment reply);
    Task<IEnumerable<ReplyComment>> GetRepliesByCommentIdAsync(int commentId);
    Task<int?> GetReplyOwnerAsync(int replyId);
    Task DeleteReplyAsync(int id);
    
    // Media operations
    Task<CommunityPostMedia> AddMediaAsync(CommunityPostMedia media);
    Task<IEnumerable<CommunityPostMedia>> GetMediaByPostIdAsync(int postId);
}

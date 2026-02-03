using Community.Domain.Entities;

namespace Community.Application.Interfaces;

public interface ICommunityService
{
    // Post operations
    Task<CommunityPost?> GetPostByIdAsync(int id);
    Task<IEnumerable<CommunityPost>> GetAllPostsAsync();
    Task<IEnumerable<CommunityPost>> GetPostsByUserIdAsync(int userId);
    Task<CommunityPost> CreatePostAsync(CommunityPost post);
    Task UpdatePostAsync(CommunityPost post);
    Task DeletePostAsync(int id);
    
    // Interaction operations
    Task<bool> SavePostAsync(int postId, int userId);
    Task<bool> UnsavePostAsync(int postId, int userId);
    Task<bool> FavoritePostAsync(int postId, int userId);
    Task<bool> UnfavoritePostAsync(int postId, int userId);
    Task<IEnumerable<CommunityPost>> GetSavedPostsAsync(int userId);
    Task<IEnumerable<CommunityPost>> GetFavoritedPostsAsync(int userId);
    
    // Comment operations
    Task<Comment> AddCommentAsync(int postId, int userId, string content);
    Task<IEnumerable<Comment>> GetCommentsAsync(int postId);
    Task DeleteCommentAsync(int commentId);
    
    // Reply operations
    Task<ReplyComment> AddReplyAsync(int commentId, int userId, int? replyToUserId, string content);
    Task<IEnumerable<ReplyComment>> GetRepliesAsync(int commentId);
    Task DeleteReplyAsync(int replyId);
}

using Community.Application.Interfaces;
using Community.Domain.Entities;

namespace Community.Application.Services;

public class CommunityService : ICommunityService
{
    private readonly ICommunityRepository _repository;

    public CommunityService(ICommunityRepository repository)
    {
        _repository = repository;
    }

    // Post operations
    public async Task<CommunityPost?> GetPostByIdAsync(int id)
    {
        return await _repository.GetPostByIdAsync(id);
    }

    public async Task<IEnumerable<CommunityPost>> GetAllPostsAsync()
    {
        return await _repository.GetAllPostsAsync();
    }

    public async Task<IEnumerable<CommunityPost>> GetPostsByUserIdAsync(int userId)
    {
        return await _repository.GetPostsByUserIdAsync(userId);
    }

    public async Task<CommunityPost> CreatePostAsync(CommunityPost post)
    {
        return await _repository.CreatePostAsync(post);
    }

    public async Task UpdatePostAsync(CommunityPost post)
    {
        await _repository.UpdatePostAsync(post);
    }

    public async Task DeletePostAsync(int id)
    {
        await _repository.DeletePostAsync(id);
    }

    // Interaction operations
    public async Task<bool> SavePostAsync(int postId, int userId)
    {
        return await _repository.SavePostAsync(postId, userId);
    }

    public async Task<bool> UnsavePostAsync(int postId, int userId)
    {
        return await _repository.UnsavePostAsync(postId, userId);
    }

    public async Task<bool> FavoritePostAsync(int postId, int userId)
    {
        return await _repository.FavoritePostAsync(postId, userId);
    }

    public async Task<bool> UnfavoritePostAsync(int postId, int userId)
    {
        return await _repository.UnfavoritePostAsync(postId, userId);
    }

    public async Task<IEnumerable<CommunityPost>> GetSavedPostsAsync(int userId)
    {
        return await _repository.GetSavedPostsByUserAsync(userId);
    }

    public async Task<IEnumerable<CommunityPost>> GetFavoritedPostsAsync(int userId)
    {
        return await _repository.GetFavoritedPostsByUserAsync(userId);
    }

    // Comment operations
    public async Task<Comment> AddCommentAsync(int postId, int userId, string content)
    {
        var comment = new Comment
        {
            CommunityPostId = postId,
            UserId = userId,
            Content = content
        };
        return await _repository.AddCommentAsync(comment);
    }

    public async Task<IEnumerable<Comment>> GetCommentsAsync(int postId)
    {
        return await _repository.GetCommentsByPostIdAsync(postId);
    }

    public async Task DeleteCommentAsync(int commentId)
    {
        await _repository.DeleteCommentAsync(commentId);
    }

    // Reply operations
    public async Task<ReplyComment> AddReplyAsync(int commentId, int userId, int? replyToUserId, string content)
    {
        var reply = new ReplyComment
        {
            CommentId = commentId,
            UserId = userId,
            ReplyToUserId = replyToUserId,
            Content = content
        };
        return await _repository.AddReplyAsync(reply);
    }

    public async Task<IEnumerable<ReplyComment>> GetRepliesAsync(int commentId)
    {
        return await _repository.GetRepliesByCommentIdAsync(commentId);
    }

    public async Task DeleteReplyAsync(int replyId)
    {
        await _repository.DeleteReplyAsync(replyId);
    }
}

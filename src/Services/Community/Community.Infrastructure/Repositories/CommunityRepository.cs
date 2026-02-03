using Microsoft.EntityFrameworkCore;
using Community.Application.Interfaces;
using Community.Domain.Entities;
using Community.Infrastructure.Data;

namespace Community.Infrastructure.Repositories;

public class CommunityRepository : ICommunityRepository
{
    private readonly CommunityDbContext _context;

    public CommunityRepository(CommunityDbContext context)
    {
        _context = context;
    }

    // CommunityPost operations
    public async Task<CommunityPost?> GetPostByIdAsync(int id)
    {
        return await _context.CommunityPosts
            .Include(p => p.Media)
            .Include(p => p.Comments)
                .ThenInclude(c => c.Replies)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IEnumerable<CommunityPost>> GetAllPostsAsync()
    {
        return await _context.CommunityPosts
            .Include(p => p.Media)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<CommunityPost>> GetPostsByUserIdAsync(int userId)
    {
        return await _context.CommunityPosts
            .Include(p => p.Media)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<CommunityPost> CreatePostAsync(CommunityPost post)
    {
        _context.CommunityPosts.Add(post);
        await _context.SaveChangesAsync();
        return post;
    }

    public async Task UpdatePostAsync(CommunityPost post)
    {
        _context.CommunityPosts.Update(post);
        await _context.SaveChangesAsync();
    }

    public async Task DeletePostAsync(int id)
    {
        var post = await _context.CommunityPosts.FindAsync(id);
        if (post != null)
        {
            _context.CommunityPosts.Remove(post);
            await _context.SaveChangesAsync();
        }
    }

    // Save/Favorite operations
    public async Task<bool> SavePostAsync(int postId, int userId)
    {
        var existing = await _context.CommunityPostSaves
            .FirstOrDefaultAsync(s => s.CommunityPostId == postId && s.UserId == userId);
        
        if (existing != null) return false;

        _context.CommunityPostSaves.Add(new CommunityPostSave
        {
            CommunityPostId = postId,
            UserId = userId
        });
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UnsavePostAsync(int postId, int userId)
    {
        var save = await _context.CommunityPostSaves
            .FirstOrDefaultAsync(s => s.CommunityPostId == postId && s.UserId == userId);
        
        if (save == null) return false;

        _context.CommunityPostSaves.Remove(save);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> FavoritePostAsync(int postId, int userId)
    {
        var existing = await _context.CommunityPostFavorites
            .FirstOrDefaultAsync(f => f.CommunityPostId == postId && f.UserId == userId);
        
        if (existing != null) return false;

        _context.CommunityPostFavorites.Add(new CommunityPostFavorite
        {
            CommunityPostId = postId,
            UserId = userId
        });

        // Increment favorite count
        var post = await _context.CommunityPosts.FindAsync(postId);
        if (post != null)
        {
            post.FavoriteCount++;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UnfavoritePostAsync(int postId, int userId)
    {
        var favorite = await _context.CommunityPostFavorites
            .FirstOrDefaultAsync(f => f.CommunityPostId == postId && f.UserId == userId);
        
        if (favorite == null) return false;

        _context.CommunityPostFavorites.Remove(favorite);

        // Decrement favorite count
        var post = await _context.CommunityPosts.FindAsync(postId);
        if (post != null && post.FavoriteCount > 0)
        {
            post.FavoriteCount--;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<CommunityPost>> GetSavedPostsByUserAsync(int userId)
    {
        return await _context.CommunityPostSaves
            .Where(s => s.UserId == userId)
            .Include(s => s.CommunityPost)
                .ThenInclude(p => p.Media)
            .Select(s => s.CommunityPost)
            .ToListAsync();
    }

    public async Task<IEnumerable<CommunityPost>> GetFavoritedPostsByUserAsync(int userId)
    {
        return await _context.CommunityPostFavorites
            .Where(f => f.UserId == userId)
            .Include(f => f.CommunityPost)
                .ThenInclude(p => p.Media)
            .Select(f => f.CommunityPost)
            .ToListAsync();
    }

    // Comment operations
    public async Task<Comment> AddCommentAsync(Comment comment)
    {
        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();
        return comment;
    }

    public async Task<IEnumerable<Comment>> GetCommentsByPostIdAsync(int postId)
    {
        return await _context.Comments
            .Where(c => c.CommunityPostId == postId)
            .Include(c => c.Replies)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task DeleteCommentAsync(int id)
    {
        var comment = await _context.Comments.FindAsync(id);
        if (comment != null)
        {
            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();
        }
    }

    // Reply operations
    public async Task<ReplyComment> AddReplyAsync(ReplyComment reply)
    {
        _context.ReplyComments.Add(reply);
        await _context.SaveChangesAsync();
        return reply;
    }

    public async Task<IEnumerable<ReplyComment>> GetRepliesByCommentIdAsync(int commentId)
    {
        return await _context.ReplyComments
            .Where(r => r.CommentId == commentId)
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task DeleteReplyAsync(int id)
    {
        var reply = await _context.ReplyComments.FindAsync(id);
        if (reply != null)
        {
            _context.ReplyComments.Remove(reply);
            await _context.SaveChangesAsync();
        }
    }

    // Media operations
    public async Task<CommunityPostMedia> AddMediaAsync(CommunityPostMedia media)
    {
        _context.CommunityPostMedia.Add(media);
        await _context.SaveChangesAsync();
        return media;
    }

    public async Task<IEnumerable<CommunityPostMedia>> GetMediaByPostIdAsync(int postId)
    {
        return await _context.CommunityPostMedia
            .Where(m => m.CommunityPostId == postId)
            .ToListAsync();
    }
}

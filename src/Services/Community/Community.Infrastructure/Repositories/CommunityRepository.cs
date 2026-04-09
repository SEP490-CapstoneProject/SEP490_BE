using Microsoft.EntityFrameworkCore;
using Community.Application.DTOs;
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

    // ─── Feed (N+1-free, cursor-based) ───────────────────────────────────────

    public async Task<List<CommunityPost>> GetFeedAsync(int? cursor, int pageSize)
    {
        IQueryable<CommunityPost> query = _context.CommunityPosts
            .Where(p => p.Status == 1)
            .Include(p => p.Media);

        if (cursor.HasValue)
            query = query.Where(p => p.Id < cursor.Value);

        return await query
            .OrderByDescending(p => p.Id)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<FeedCountsResult> GetFeedCountsAsync(List<int> postIds, int? currentUserId)
    {
        // Execute queries sequentially to avoid DbContext concurrency issues
        var commentCounts = await _context.Comments
            .AsNoTracking()
            .Where(c => postIds.Contains(c.CommunityPostId))
            .GroupBy(c => c.CommunityPostId)
            .Select(g => new { PostId = g.Key, Count = g.Count() })
            .ToListAsync();

        var favoritedPostIds = currentUserId.HasValue
            ? await _context.CommunityPostFavorites
                .AsNoTracking()
                .Where(f => f.UserId == currentUserId.Value && postIds.Contains(f.CommunityPostId))
                .Select(f => f.CommunityPostId)
                .ToListAsync()
            : new List<int>();

        var savedPostIds = currentUserId.HasValue
            ? await _context.CommunityPostSaves
                .AsNoTracking()
                .Where(s => s.UserId == currentUserId.Value && postIds.Contains(s.CommunityPostId))
                .Select(s => s.CommunityPostId)
                .ToListAsync()
            : new List<int>();

        return new FeedCountsResult
        {
            CommentCounts = commentCounts.ToDictionary(x => x.PostId, x => x.Count),
            FavoritedPostIds = new HashSet<int>(favoritedPostIds),
            SavedPostIds = new HashSet<int>(savedPostIds)
        };
    }

    // ─── CommunityPost operations ─────────────────────────────────────────────

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

    // ─── Save/Favorite operations ─────────────────────────────────────────────

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

        var post = await _context.CommunityPosts.FindAsync(postId);
        if (post != null) post.FavoriteCount++;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UnfavoritePostAsync(int postId, int userId)
    {
        var favorite = await _context.CommunityPostFavorites
            .FirstOrDefaultAsync(f => f.CommunityPostId == postId && f.UserId == userId);
        
        if (favorite == null) return false;

        _context.CommunityPostFavorites.Remove(favorite);

        var post = await _context.CommunityPosts.FindAsync(postId);
        if (post != null && post.FavoriteCount > 0) post.FavoriteCount--;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<int> GetPostFavoriteCountAsync(int postId)
    {
        var post = await _context.CommunityPosts
            .AsNoTracking()
            .Where(p => p.Id == postId)
            .Select(p => p.FavoriteCount)
            .FirstOrDefaultAsync();
        
        return post;
    }

    public async Task<IEnumerable<CommunityPost>> GetSavedPostsByUserAsync(int userId)
    {
        return await _context.CommunityPostSaves
            .Where(s => s.UserId == userId)
            .Include(s => s.CommunityPost)
                .ThenInclude(p => p.Media)
            .OrderByDescending(s => s.CommunityPost.CreatedAt)
            .Select(s => s.CommunityPost)
            .ToListAsync();
    }

    public async Task<IEnumerable<CommunityPost>> GetFavoritedPostsByUserAsync(int userId)
    {
        return await _context.CommunityPostFavorites
            .Where(f => f.UserId == userId)
            .Include(f => f.CommunityPost)
                .ThenInclude(p => p.Media)
            .OrderByDescending(f => f.CommunityPost.CreatedAt)
            .Select(f => f.CommunityPost)
            .ToListAsync();
    }

    // ─── Comment operations ───────────────────────────────────────────────────

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
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<ReplyComment>> GetRepliesByCommentIdsAsync(List<int> commentIds)
    {
        return await _context.ReplyComments
            .Where(r => commentIds.Contains(r.CommentId))
            .OrderBy(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<Comment?> GetCommentByIdAsync(int commentId)
    {
        return await _context.Comments.FirstOrDefaultAsync(c => c.Id == commentId);
    }

    public async Task<int?> GetCommentOwnerAsync(int commentId)
    {
        return await _context.Comments
            .Where(c => c.Id == commentId)
            .Select(c => (int?)c.UserId)
            .FirstOrDefaultAsync();
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

    // ─── Reply operations ─────────────────────────────────────────────────────

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
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<int?> GetReplyOwnerAsync(int replyId)
    {
        return await _context.ReplyComments
            .Where(r => r.Id == replyId)
            .Select(r => (int?)r.UserId)
            .FirstOrDefaultAsync();
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

    // ─── Media operations ─────────────────────────────────────────────────────

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

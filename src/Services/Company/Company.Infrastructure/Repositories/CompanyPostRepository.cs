using Company.Application.DTOs;
using Company.Application.Interfaces;
using Company.Domain.Entities;
using Company.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Company.Infrastructure.Repositories;

public class CompanyPostRepository : ICompanyPostRepository
{
    private readonly CompanyDbContext _context;

    public CompanyPostRepository(CompanyDbContext context)
    {
        _context = context;
    }

    public async Task<CursorPagedResult<CompanyPostFeedDto>> GetPostFeedAsync(DateTime? cursor, int limit, int? userId)
    {
        return await BuildFeedQuery(null, cursor, limit, userId);
    }

    public async Task<CursorPagedResult<CompanyPostFeedDto>> GetPostsByCompanyAsync(int companyId, DateTime? cursor, int limit, int? userId)
    {
        return await BuildFeedQuery(companyId, cursor, limit, userId);
    }

    public async Task<CursorPagedResult<CompanyPostFeedDto>> GetSavedPostsAsync(DateTime? cursor, int limit, int userId)
    {
        var query = _context.CompanyPostSaves
            .Where(s => s.UserId == userId)
            .Join(
                _context.CompanyPosts.Where(p => p.Status == 1),
                s => s.CompanyPostId,
                p => p.PostId,
                (s, p) => p);

        if (cursor.HasValue)
            query = query.Where(p => p.CreatedAt < cursor.Value);

        var items = await (
                from p in query
                join c in _context.Companies on p.CompanyId equals c.Id into pc
                from c in pc.DefaultIfEmpty()
                orderby p.CreatedAt descending, p.PostId descending
                select new CompanyPostFeedDto
                {
                    CompanyId = p.CompanyId,
                    PostId = p.PostId,
                    Position = p.Position,
                    CompanyName = c != null ? c.Name : null,
                    CompanyAvatar = c != null ? c.AvatarUrl : null,
                    CoverImageUrl = p.CoverImageVideo,
                    MediaType = p.Media.OrderBy(m => m.Id).Select(m => m.Type).FirstOrDefault(),
                    MediaUrl = p.Media.OrderBy(m => m.Id).Select(m => m.Address).FirstOrDefault(),
                    Address = p.Address,
                    Salary = p.Salary,
                    EmploymentType = p.EmploymentType,
                    CreatedAt = p.CreatedAt,
                    IsSaved = true
                })
            .Take(limit + 1)
            .ToListAsync();

        var hasMore = items.Count > limit;
        if (hasMore) items.RemoveAt(items.Count - 1);

        return new CursorPagedResult<CompanyPostFeedDto>
        {
            Items = items,
            NextCursor = hasMore ? items.LastOrDefault()?.CreatedAt : null,
            HasMore = hasMore
        };
    }

    public async Task<List<CompanyPostDetailDto>> GetPostsByIdsAsync(List<int> postIds, int? userId)
    {
        var normalizedIds = postIds.Where(x => x > 0).Distinct().ToList();
        if (normalizedIds.Count == 0)
        {
            return new List<CompanyPostDetailDto>();
        }

        var posts = await _context.CompanyPosts
            .Include(p => p.Media.OrderBy(m => m.Id))
            .Where(p => normalizedIds.Contains(p.PostId) && p.Status == 1)
            .ToListAsync();

        var companyIds = posts.Select(p => p.CompanyId).Distinct().ToList();
        var companies = await _context.Companies
            .Where(c => companyIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id);

        HashSet<int> savedPostIds = new();
        if (userId.HasValue)
        {
            var savedIds = await _context.CompanyPostSaves
                .Where(s => s.UserId == userId.Value && normalizedIds.Contains(s.CompanyPostId))
                .Select(s => s.CompanyPostId)
                .ToListAsync();
            savedPostIds = savedIds.ToHashSet();
        }

        var orderMap = normalizedIds
            .Select((postId, index) => new { postId, index })
            .ToDictionary(x => x.postId, x => x.index);

        return posts
            .Select(post =>
            {
                companies.TryGetValue(post.CompanyId, out var company);
                return new CompanyPostDetailDto
                {
                    PostId = post.PostId,
                    CompanyId = post.CompanyId,
                    Position = post.Position,
                    CompanyName = company?.Name,
                    CompanyAvatar = company?.AvatarUrl,
                    CoverImageUrl = post.CoverImageVideo,
                    Address = post.Address,
                    Salary = post.Salary,
                    EmploymentType = post.EmploymentType,
                    ExperienceYear = post.ExperienceYear,
                    Quantity = post.Quantity,
                    JobDescription = post.JobDescription,
                    RequirementsMandatory = post.RequirementsMandatory,
                    RequirementsPreferred = post.RequirementsPreferred,
                    Benefits = post.Benefits,
                    CreatedAt = post.CreatedAt,
                    Status = post.Status,
                    IsSaved = savedPostIds.Contains(post.PostId),
                    Media = post.Media.Select(m => new MediaItemDto
                    {
                        Type = m.Type,
                        Url = m.Address
                    }).ToList()
                };
            })
            .OrderBy(p => orderMap[p.PostId])
            .ToList();
    }

    private async Task<CursorPagedResult<CompanyPostFeedDto>> BuildFeedQuery(int? companyId, DateTime? cursor, int limit, int? userId)
    {
        var query = _context.CompanyPosts
            .Where(p => p.Status == 1);

        if (companyId.HasValue)
            query = query.Where(p => p.CompanyId == companyId.Value);

        if (cursor.HasValue)
            query = query.Where(p => p.CreatedAt < cursor.Value);

        var items = await (
                from p in query
                join c in _context.Companies on p.CompanyId equals c.Id into pc
                from c in pc.DefaultIfEmpty()
                orderby p.CreatedAt descending, p.PostId descending
                select new CompanyPostFeedDto
                {
                    CompanyId = p.CompanyId,
                    PostId = p.PostId,
                    Position = p.Position,
                    CompanyName = c != null ? c.Name : null,
                    CompanyAvatar = c != null ? c.AvatarUrl : null,
                    CoverImageUrl = p.CoverImageVideo,
                    MediaType = p.Media.OrderBy(m => m.Id).Select(m => m.Type).FirstOrDefault(),
                    MediaUrl = p.Media.OrderBy(m => m.Id).Select(m => m.Address).FirstOrDefault(),
                    Address = p.Address,
                    Salary = p.Salary,
                    EmploymentType = p.EmploymentType,
                    CreatedAt = p.CreatedAt,
                    IsSaved = userId.HasValue && _context.CompanyPostSaves.Any(s => s.UserId == userId.Value && s.CompanyPostId == p.PostId)
                })
            .Take(limit + 1)
            .ToListAsync();

        var hasMore = items.Count > limit;
        if (hasMore) items.RemoveAt(items.Count - 1);

        return new CursorPagedResult<CompanyPostFeedDto>
        {
            Items = items,
            NextCursor = hasMore ? items.LastOrDefault()?.CreatedAt : null,
            HasMore = hasMore
        };
    }

    public async Task<CompanyPostDetailDto?> GetPostDetailAsync(int postId, int? userId)
    {
        var post = await _context.CompanyPosts
            .AsNoTracking()
            .Include(p => p.Media.OrderBy(m => m.Id))
            .FirstOrDefaultAsync(p => p.PostId == postId && p.Status == 1);

        if (post == null) return null;

        var company = await _context.Companies
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == post.CompanyId);

        var isSaved = userId.HasValue &&
            await _context.CompanyPostSaves.AnyAsync(s => s.UserId == userId.Value && s.CompanyPostId == postId);

        return new CompanyPostDetailDto
        {
            PostId = post.PostId,
            CompanyId = post.CompanyId,
            Position = post.Position,
            CompanyName = company?.Name,
            CompanyAvatar = company?.AvatarUrl,
            CoverImageUrl = post.CoverImageVideo,
            Address = post.Address,
            Salary = post.Salary,
            EmploymentType = post.EmploymentType,
            ExperienceYear = post.ExperienceYear,
            Quantity = post.Quantity,
            JobDescription = post.JobDescription,
            RequirementsMandatory = post.RequirementsMandatory,
            RequirementsPreferred = post.RequirementsPreferred,
            Benefits = post.Benefits,
            CreatedAt = post.CreatedAt,
            Status = post.Status,
            IsSaved = isSaved,
            Media = post.Media.Select(m => new MediaItemDto
            {
                Type = m.Type,
                Url = m.Address
            }).ToList()
        };
    }

    public async Task<bool> CheckPostSavedAsync(int userId, int postId)
        => await _context.CompanyPostSaves.AnyAsync(s => s.UserId == userId && s.CompanyPostId == postId);

    public async Task SavePostAsync(int userId, int postId)
    {
        var exists = await CheckPostSavedAsync(userId, postId);
        if (exists) return;

        _context.CompanyPostSaves.Add(new CompanyPostSave
        {
            UserId = userId,
            CompanyPostId = postId
        });
        await _context.SaveChangesAsync();
    }

    public async Task UnsavePostAsync(int userId, int postId)
    {
        var save = await _context.CompanyPostSaves
            .FirstOrDefaultAsync(s => s.UserId == userId && s.CompanyPostId == postId);

        if (save == null) return;

        _context.CompanyPostSaves.Remove(save);
        await _context.SaveChangesAsync();
    }

    public async Task<CompanyPost> CreatePostAsync(CompanyPost post)
    {
        _context.CompanyPosts.Add(post);
        await _context.SaveChangesAsync();
        return post;
    }

    public async Task UpdatePostAsync(CompanyPost post)
    {
        _context.CompanyPosts.Update(post);
        await _context.SaveChangesAsync();
    }

    public async Task RemovePostMediaAsync(int postId)
    {
        await _context.CompanyPostMedia
            .Where(m => m.CompanyPostId == postId)
            .ExecuteDeleteAsync();
    }

    public async Task SoftDeletePostAsync(int postId)
    {
        var post = await _context.CompanyPosts.FindAsync(postId);
        if (post == null) return;

        post.Status = 0;
        await _context.SaveChangesAsync();
    }

    public async Task AddPostMediaAsync(CompanyPostMedia media)
    {
        _context.CompanyPostMedia.Add(media);
        await _context.SaveChangesAsync();
    }
}

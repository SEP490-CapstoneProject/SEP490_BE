using Company.Application.DTOs;
using Company.Application.Interfaces;
using Company.Domain.Entities;
using Company.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using RecruitmentPlatform.AI.Models;

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

    public async Task<CompanyPost?> GetPostEntityByIdAsync(int postId)
        => await _context.CompanyPosts.FirstOrDefaultAsync(p => p.PostId == postId && p.Status == 1);

    public async Task<List<CompanyPost>> GetActivePostsForMatchingAsync(int limit)
    {
        var safeLimit = Math.Clamp(limit, 1, 150);
        return await _context.CompanyPosts
            .AsNoTracking()
            .Where(p => p.Status == 1
                && p.Embedding != null
                && p.EmbeddingStatus == "Ready")
            .OrderByDescending(p => p.EmbeddingUpdatedAt ?? p.CreatedAt)
            .ThenByDescending(p => p.PostId)
            .Take(safeLimit)
            .ToListAsync();
    }

    public async Task<List<CompanyPost>> GetPostsForEmbeddingBackfillAsync(int limit)
    {
        var safeLimit = Math.Clamp(limit, 1, 200);
        return await _context.CompanyPosts
            .Where(p => p.Status == 1 && (p.Embedding == null || p.EmbeddingStatus != EmbeddingReadinessPolicy.Ready))
            .OrderByDescending(p => p.EmbeddingUpdatedAt ?? p.CreatedAt)
            .ThenByDescending(p => p.PostId)
            .Take(safeLimit)
            .ToListAsync();
    }

    public async Task UpdateEmbeddingAsync(int postId, string? embedding, int embeddingVersion, DateTime? embeddingUpdatedAt, string embeddingStatus)
    {
        var post = await _context.CompanyPosts.FirstOrDefaultAsync(p => p.PostId == postId);
        if (post == null) return;

        post.Embedding = embedding;
        post.EmbeddingVersion = embeddingVersion;
        post.EmbeddingUpdatedAt = embeddingUpdatedAt;
        post.EmbeddingStatus = embeddingStatus;
        await _context.SaveChangesAsync();
    }

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

    public async Task<CompanyPost?> GetByIdAsync(int postId)
    {
        return await _context.CompanyPosts.FindAsync(postId);
    }

    public async Task<List<CompanyPost>> GetPendingPostsAsync(int pageNumber, int pageSize)
    {
        var skip = (pageNumber - 1) * pageSize;
        return await _context.CompanyPosts
            .Where(p => p.ReviewStatus == 3) // PendingReview
            .OrderByDescending(p => p.ReviewedAt)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();
    }

    public async Task<CompanyPostReport> CreatePostReportAsync(CompanyPostReport report)
    {
        _context.CompanyPostReports.Add(report);
        await _context.SaveChangesAsync();
        return report;
    }

    public async Task<CompanyPostReport?> GetPostReportByPostAndReporterAsync(int postId, int reporterUserId)
    {
        return await _context.CompanyPostReports
            .FirstOrDefaultAsync(r => r.CompanyPostId == postId && r.ReporterUserId == reporterUserId);
    }

    public async Task<(List<CompanyPostReport> Items, int Total)> GetPostReportsAsync(int page, int pageSize)
    {
        var safePage = Math.Max(1, page);
        var safePageSize = Math.Clamp(pageSize, 1, 100);

        var query = _context.CompanyPostReports
            .AsNoTracking()
            .Include(r => r.CompanyPost)
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.Id);

        var total = await query.CountAsync();
        var items = await query
            .Skip((safePage - 1) * safePageSize)
            .Take(safePageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<CompanyPostReport?> GetPostReportByIdAsync(int reportId)
    {
        return await _context.CompanyPostReports
            .Include(r => r.CompanyPost)
            .FirstOrDefaultAsync(r => r.Id == reportId);
    }

    public async Task UpdatePostReportAsync(CompanyPostReport report)
    {
        _context.CompanyPostReports.Update(report);
        await _context.SaveChangesAsync();
    }

    public async Task<PagedResult<CompanyPostFeedDto>> SearchPostsAsync(
        string? q, string? position, string? salary, string? location, string? employmentType, string? level,
        string? q_position, string? q_description, string? q_requirements,
        int skip, int take, int? userId)
    {
        var query = _context.CompanyPosts.Where(p => p.Status == 1);

        // Full-text search on keywords across all searchable company post fields
        // Split keywords and search for ALL of them (AND logic)
        if (!string.IsNullOrWhiteSpace(q))
        {
            var keywordArray = q.Trim().ToLower().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            
            // Each keyword must appear in at least one of the searchable fields
            foreach (var keyword in keywordArray)
            {
                var keywordFilter = keyword;
                query = query.Where(p =>
                    p.Position.ToLower().Contains(keywordFilter) ||
                    (p.Address != null && p.Address.ToLower().Contains(keywordFilter)) ||
                    (p.Salary != null && p.Salary.ToLower().Contains(keywordFilter)) ||
                    (p.EmploymentType != null && p.EmploymentType.ToLower().Contains(keywordFilter)) ||
                    p.JobDescription.ToLower().Contains(keywordFilter) ||
                    p.RequirementsMandatory.ToLower().Contains(keywordFilter) ||
                    (p.RequirementsPreferred != null && p.RequirementsPreferred.ToLower().Contains(keywordFilter)) ||
                    (p.Benefits != null && p.Benefits.ToLower().Contains(keywordFilter)) ||
                    (p.ExperienceYear.HasValue && p.ExperienceYear.Value.ToString().Contains(keywordFilter)) ||
                    (p.Quantity.HasValue && p.Quantity.Value.ToString().Contains(keywordFilter)));
            }
        }

        // Advanced search: specific field searches
        if (!string.IsNullOrWhiteSpace(q_position))
        {
            var posKeywords = q_position.Trim().ToLower().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var keyword in posKeywords)
            {
                var keywordFilter = keyword;
                query = query.Where(p => p.Position.ToLower().Contains(keywordFilter));
            }
        }

        if (!string.IsNullOrWhiteSpace(q_description))
        {
            var descKeywords = q_description.Trim().ToLower().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var keyword in descKeywords)
            {
                var keywordFilter = keyword;
                query = query.Where(p => p.JobDescription.ToLower().Contains(keywordFilter));
            }
        }

        if (!string.IsNullOrWhiteSpace(q_requirements))
        {
            var reqKeywords = q_requirements.Trim().ToLower().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var keyword in reqKeywords)
            {
                var keywordFilter = keyword;
                query = query.Where(p => p.RequirementsMandatory.ToLower().Contains(keywordFilter));
            }
        }

        // Filter by position
        if (!string.IsNullOrWhiteSpace(position))
        {
            var posFilter = position.Trim().ToLower();
            query = query.Where(p => p.Position.ToLower().Contains(posFilter));
        }

        // Filter by location
        if (!string.IsNullOrWhiteSpace(location))
        {
            var locFilter = location.Trim().ToLower();
            query = query.Where(p => p.Address.ToLower().Contains(locFilter));
        }

        // Filter by employment type
        if (!string.IsNullOrWhiteSpace(employmentType))
        {
            var typeFilter = employmentType.Trim();
            query = query.Where(p => p.EmploymentType == typeFilter);
        }

        // Filter by experience level
        if (!string.IsNullOrWhiteSpace(level))
        {
            var levelFilter = level.Trim().ToLower();
            // Assuming level is stored in RequirementsMandatory or ExperienceYear
            query = query.Where(p =>
                p.RequirementsMandatory.ToLower().Contains(levelFilter) ||
                p.ExperienceYear.ToString().Contains(levelFilter));
        }

        // Filter by salary range (format: "min-max", e.g. "5000-20000")
        // Note: Salary filtering is applied client-side because EF Core can't use out parameters in expressions
        decimal? minSalary = null;
        decimal? maxSalary = null;
        if (!string.IsNullOrWhiteSpace(salary))
        {
            var salaryParts = salary.Split('-');
            if (salaryParts.Length == 2)
            {
                if (decimal.TryParse(salaryParts[0].Trim(), out var min))
                    minSalary = min;
                if (decimal.TryParse(salaryParts[1].Trim(), out var max))
                    maxSalary = max;
            }
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync();

        // Apply pagination: convert skip/take to page/pageSize
        var safeTake = Math.Clamp(take, 1, 100);
        var safeSkip = Math.Max(0, skip);
        var page = (safeSkip / safeTake) + 1;

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
            .Skip(safeSkip)
            .Take(safeTake)
            .ToListAsync();

        // Apply client-side salary filtering
        if (minSalary.HasValue || maxSalary.HasValue)
        {
            items = items.Where(item =>
            {
                if (!decimal.TryParse(item.Salary, out var itemSalary))
                    return false;
                
                if (minSalary.HasValue && itemSalary < minSalary.Value)
                    return false;
                if (maxSalary.HasValue && itemSalary > maxSalary.Value)
                    return false;
                
                return true;
            }).ToList();
        }

        return new PagedResult<CompanyPostFeedDto>
        {
            Items = items,
            Total = totalCount,
            Page = page,
            PageSize = safeTake
        };
    }
}

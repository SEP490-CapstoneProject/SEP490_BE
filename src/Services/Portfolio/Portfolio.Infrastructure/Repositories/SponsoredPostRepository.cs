using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Interfaces;
using Portfolio.Domain.Entities;
using Portfolio.Infrastructure.Data;

namespace Portfolio.Infrastructure.Repositories;

public class SponsoredPostRepository : ISponsoredPostRepository
{
    private readonly PortfolioDbContext _context;

    public SponsoredPostRepository(PortfolioDbContext context) => _context = context;

    public async Task<SponsoredPost?> GetByIdAsync(int id)
        => await _context.SponsoredPosts
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id && p.Status != SponsoredPostStatus.Deleted);

    public async Task<List<SponsoredPost>> GetByCreatorAsync(int createdBy)
        => await _context.SponsoredPosts
            .AsNoTracking()
            .Where(p => p.CreatedBy == createdBy && p.Status != SponsoredPostStatus.Deleted)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

    public async Task<List<SponsoredPost>> GetActivePostsAsync()
    {
        var now = DateTime.UtcNow;
        return await _context.SponsoredPosts
            .AsNoTracking()
            .Where(p => p.Status == SponsoredPostStatus.Active 
                     && p.StartDate <= now 
                     && p.ExpiryDate > now)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();
    }

    public async Task<List<SponsoredPost>> GetActivePostsPagedAsync(int skip, int take)
    {
        var now = DateTime.UtcNow;
        return await _context.SponsoredPosts
            .AsNoTracking()
            .Where(p => p.Status == SponsoredPostStatus.Active 
                     && p.StartDate <= now 
                     && p.ExpiryDate > now)
            .OrderByDescending(p => p.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<SponsoredPost> CreateAsync(SponsoredPost post)
    {
        _context.SponsoredPosts.Add(post);
        await _context.SaveChangesAsync();
        return post;
    }

    public async Task<SponsoredPost> UpdateAsync(SponsoredPost post)
    {
        post.UpdatedAt = DateTime.UtcNow;
        _context.SponsoredPosts.Update(post);
        await _context.SaveChangesAsync();
        return post;
    }

    public async Task DeleteAsync(int id)
    {
        await _context.SponsoredPosts
            .Where(p => p.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Status, SponsoredPostStatus.Deleted)
                .SetProperty(p => p.UpdatedAt, DateTime.UtcNow));
    }

    public async Task<int> GetActivePostCountAsync(int createdBy)
    {
        var now = DateTime.UtcNow;
        return await _context.SponsoredPosts
            .CountAsync(p => p.CreatedBy == createdBy 
                          && p.Status == SponsoredPostStatus.Active
                          && p.StartDate <= now 
                          && p.ExpiryDate > now);
    }
}

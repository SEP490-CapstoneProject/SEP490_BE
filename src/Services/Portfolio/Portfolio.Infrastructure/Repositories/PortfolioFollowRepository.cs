using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Interfaces;
using Portfolio.Domain.Entities;
using Portfolio.Infrastructure.Data;

namespace Portfolio.Infrastructure.Repositories;

public class PortfolioFollowRepository : IPortfolioFollowRepository
{
    private readonly PortfolioDbContext _context;

    public PortfolioFollowRepository(PortfolioDbContext context)
    {
        _context = context;
    }

    public async Task<PortfolioFollow?> GetByCompanyAndPortfolioAsync(int companyId, int portfolioId)
        => await _context.PortfolioFollows
            .Include(x => x.Portfolio)
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.PortfolioId == portfolioId);

    public async Task<List<PortfolioFollow>> GetByCompanyAsync(int companyId, int? categoryId = null)
    {
        var query = _context.PortfolioFollows
            .Where(x => x.CompanyId == companyId);

        if (categoryId.HasValue)
        {
            query = query.Where(x => x.CategoryId == categoryId.Value);
        }

        return await query
            .Include(x => x.Portfolio)
            .Include(x => x.Category)
            .OrderByDescending(x => x.FollowedAt)
            .ToListAsync();
    }

    public async Task<HashSet<int>> GetFollowedPortfolioIdsAsync(int companyId, IEnumerable<int> portfolioIds)
    {
        var ids = portfolioIds.Distinct().ToList();
        if (ids.Count == 0)
        {
            return new HashSet<int>();
        }

        var followedIds = await _context.PortfolioFollows
            .AsNoTracking()
            .Where(x => x.CompanyId == companyId && ids.Contains(x.PortfolioId))
            .Select(x => x.PortfolioId)
            .ToListAsync();

        return followedIds.ToHashSet();
    }

    public async Task<PortfolioFollow> CreateAsync(PortfolioFollow follow)
    {
        _context.PortfolioFollows.Add(follow);
        await _context.SaveChangesAsync();
        return follow;
    }

    public async Task<PortfolioFollow> UpdateAsync(PortfolioFollow follow)
    {
        _context.PortfolioFollows.Update(follow);
        await _context.SaveChangesAsync();
        return follow;
    }

    public async Task DeleteAsync(PortfolioFollow follow)
    {
        _context.PortfolioFollows.Remove(follow);
        await _context.SaveChangesAsync();
    }
}

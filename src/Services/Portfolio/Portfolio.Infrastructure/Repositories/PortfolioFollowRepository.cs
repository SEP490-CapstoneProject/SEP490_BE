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
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.PortfolioId == portfolioId);

    public async Task<List<PortfolioFollow>> GetByCompanyAsync(int companyId)
        => await _context.PortfolioFollows
            .Where(x => x.CompanyId == companyId)
            .Include(x => x.Portfolio)
            .OrderByDescending(x => x.FollowedAt)
            .ToListAsync();

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

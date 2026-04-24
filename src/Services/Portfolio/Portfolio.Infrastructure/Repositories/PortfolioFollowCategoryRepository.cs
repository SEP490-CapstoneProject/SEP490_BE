using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Interfaces;
using Portfolio.Domain.Entities;
using Portfolio.Infrastructure.Data;

namespace Portfolio.Infrastructure.Repositories;

public class PortfolioFollowCategoryRepository : IPortfolioFollowCategoryRepository
{
    private readonly PortfolioDbContext _context;

    public PortfolioFollowCategoryRepository(PortfolioDbContext context)
    {
        _context = context;
    }

    public async Task<PortfolioFollowCategory?> GetByIdAsync(int categoryId)
        => await _context.PortfolioFollowCategories.FirstOrDefaultAsync(x => x.Id == categoryId);

    public async Task<PortfolioFollowCategory?> GetByCompanyAndCodeAsync(int companyId, string code)
        => await _context.PortfolioFollowCategories
            .FirstOrDefaultAsync(x => x.CompanyId == companyId && x.Code == code);

    public async Task<List<PortfolioFollowCategory>> GetByCompanyAsync(int companyId)
        => await _context.PortfolioFollowCategories
            .Where(x => x.CompanyId == companyId)
            .OrderBy(x => x.Name)
            .ToListAsync();

    public async Task<bool> IsCategoryInUseAsync(int categoryId)
        => await _context.PortfolioFollows.AnyAsync(x => x.CategoryId == categoryId);

    public async Task<PortfolioFollowCategory> CreateAsync(PortfolioFollowCategory category)
    {
        _context.PortfolioFollowCategories.Add(category);
        await _context.SaveChangesAsync();
        return category;
    }

    public async Task<PortfolioFollowCategory> UpdateAsync(PortfolioFollowCategory category)
    {
        _context.PortfolioFollowCategories.Update(category);
        await _context.SaveChangesAsync();
        return category;
    }

    public async Task DeleteAsync(PortfolioFollowCategory category)
    {
        _context.PortfolioFollowCategories.Remove(category);
        await _context.SaveChangesAsync();
    }
}

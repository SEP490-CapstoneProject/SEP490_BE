using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Interfaces;
using Portfolio.Domain.Entities;
using Portfolio.Infrastructure.Data;

namespace Portfolio.Infrastructure.Repositories;

public class PortfolioPreviewRepository : IPortfolioPreviewRepository
{
    private readonly PortfolioDbContext _context;

    public PortfolioPreviewRepository(PortfolioDbContext context)
    {
        _context = context;
    }

    public async Task<PortfolioPreview?> GetByPortfolioIdAsync(int portfolioId)
    {
        return await _context.PortfolioPreview
            .FirstOrDefaultAsync(p => p.PortfolioId == portfolioId);
    }

    public async Task<PortfolioPreview> CreateAsync(PortfolioPreview preview)
    {
        _context.PortfolioPreview.Add(preview);
        await _context.SaveChangesAsync();
        return preview;
    }

    public async Task<PortfolioPreview> UpdateAsync(PortfolioPreview preview)
    {
        _context.PortfolioPreview.Update(preview);
        await _context.SaveChangesAsync();
        return preview;
    }

    public async Task<bool> ExistsByPortfolioIdAsync(int portfolioId)
    {
        return await _context.PortfolioPreview
            .AnyAsync(p => p.PortfolioId == portfolioId);
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var preview = await _context.PortfolioPreview
            .FirstOrDefaultAsync(p => p.Id == id);

        if (preview == null)
            return false;

        _context.PortfolioPreview.Remove(preview);
        await _context.SaveChangesAsync();
        return true;
    }
}

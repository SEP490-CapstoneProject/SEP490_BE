using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Interfaces;
using Portfolio.Domain.Entities;
using Portfolio.Infrastructure.Data;

namespace Portfolio.Infrastructure.Repositories;

public class BlockRepository : IBlockRepository
{
    private readonly PortfolioDbContext _ctx;
    public BlockRepository(PortfolioDbContext ctx) => _ctx = ctx;

    public async Task<PortfolioBlock?> GetByIdWithDataAsync(int blockId)
        => await _ctx.PortfolioBlocks
            .Include(b => b.BlockType)
            .FirstOrDefaultAsync(b => b.Id == blockId);

    public async Task<List<PortfolioBlock>> GetByPortfolioIdAsync(int portfolioId)
        => await _ctx.PortfolioBlocks
            .Where(b => b.PortfolioId == portfolioId && b.IsVisible)
            .Include(b => b.BlockType)
            .OrderBy(b => b.DisplayOrder)
            .ToListAsync();

    public async Task<int> GetMaxOrderAsync(int portfolioId)
    {
        var max = await _ctx.PortfolioBlocks
            .Where(b => b.PortfolioId == portfolioId)
            .MaxAsync(b => (int?)b.DisplayOrder);
        return max ?? 0;
    }

    public async Task<int> CountByTypeAsync(int portfolioId, int blockTypeId)
        => await _ctx.PortfolioBlocks
            .CountAsync(b => b.PortfolioId == portfolioId && b.BlockTypeId == blockTypeId);

    public async Task<PortfolioBlock> CreateAsync(PortfolioBlock block)
    {
        _ctx.PortfolioBlocks.Add(block);
        await _ctx.SaveChangesAsync();
        return block;
    }

    public async Task<PortfolioBlock> UpdateAsync(PortfolioBlock block)
    {
        _ctx.PortfolioBlocks.Update(block);
        await _ctx.SaveChangesAsync();
        return block;
    }

    public async Task DeleteAsync(PortfolioBlock block)
    {
        _ctx.PortfolioBlocks.Remove(block);
        await _ctx.SaveChangesAsync();
    }

    public async Task ReorderAsync(List<(int blockId, int order)> reorders)
    {
        foreach (var (blockId, order) in reorders)
        {
            var block = await _ctx.PortfolioBlocks.FindAsync(blockId);
            if (block != null) block.DisplayOrder = order;
        }
        await _ctx.SaveChangesAsync();
    }
}

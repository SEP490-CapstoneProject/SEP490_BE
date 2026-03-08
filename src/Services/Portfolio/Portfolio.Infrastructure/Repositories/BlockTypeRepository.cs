using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Interfaces;
using Portfolio.Domain.Entities;
using Portfolio.Infrastructure.Data;

namespace Portfolio.Infrastructure.Repositories;

public class BlockTypeRepository : IBlockTypeRepository
{
    private readonly PortfolioDbContext _context;

    public BlockTypeRepository(PortfolioDbContext context)
    {
        _context = context;
    }

    public async Task<List<BlockType>> GetAllAsync()
        => await _context.BlockTypes.OrderBy(x => x.Id).ToListAsync();

    public async Task<List<BlockType>> GetActiveAsync()
        => await _context.BlockTypes.Where(x => x.IsActive).OrderBy(x => x.Id).ToListAsync();

    public async Task<BlockType?> GetByIdAsync(int id)
        => await _context.BlockTypes.FindAsync(id);

    public async Task<BlockType?> GetByCodeAsync(string code)
        => await _context.BlockTypes.FirstOrDefaultAsync(x => x.Code == code.ToUpper());

    public async Task<BlockType> CreateAsync(BlockType blockType)
    {
        _context.BlockTypes.Add(blockType);
        await _context.SaveChangesAsync();
        return blockType;
    }

    public async Task<BlockType> UpdateAsync(BlockType blockType)
    {
        _context.BlockTypes.Update(blockType);
        await _context.SaveChangesAsync();
        return blockType;
    }
}

using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Interfaces;
using Portfolio.Domain.Entities;
using Portfolio.Infrastructure.Data;

namespace Portfolio.Infrastructure.Repositories;

public class PortfolioRepository : IPortfolioRepository
{
    private readonly PortfolioDbContext _context;

    public PortfolioRepository(PortfolioDbContext context)
    {
        _context = context;
    }

    public async Task<Portfolio.Domain.Entities.Portfolio?> GetByIdAsync(int id)
        => await _context.Portfolios
            .Include(p => p.Blocks)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<IEnumerable<Portfolio.Domain.Entities.Portfolio>> GetByEmployeeIdAsync(int employeeId)
        => await _context.Portfolios
            .Where(p => p.EmployeeId == employeeId)
            .Include(p => p.Blocks)
            .ToListAsync();

    public async Task<bool> ExistsByEmployeeIdAsync(int employeeId)
        => await _context.Portfolios.AnyAsync(p => p.EmployeeId == employeeId);

    public async Task<Portfolio.Domain.Entities.Portfolio> CreateAsync(Portfolio.Domain.Entities.Portfolio portfolio)
    {
        _context.Portfolios.Add(portfolio);
        await _context.SaveChangesAsync();
        return portfolio;
    }

    public async Task<Portfolio.Domain.Entities.Portfolio> UpdateAsync(Portfolio.Domain.Entities.Portfolio portfolio)
    {
        _context.Portfolios.Update(portfolio);
        await _context.SaveChangesAsync();
        return portfolio;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var portfolio = await _context.Portfolios.FindAsync(id);
        if (portfolio == null) return false;
        _context.Portfolios.Remove(portfolio);
        await _context.SaveChangesAsync();
        return true;
    }

    public void AddAsync(Portfolio.Domain.Entities.Portfolio portfolio)
    {
        _context.Portfolios.Add(portfolio);
    }

    public async Task CommitAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<Dictionary<string, BlockType>> GetBlockTypesAsync()
        => await _context.BlockTypes.ToDictionaryAsync(x => x.Code);
}

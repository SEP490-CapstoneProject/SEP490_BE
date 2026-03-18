using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Interfaces;
using Portfolio.Domain.Entities;
using Portfolio.Infrastructure.Data;

namespace Portfolio.Infrastructure.Repositories;

public class ComplimentRepository : IComplimentRepository
{
    private readonly PortfolioDbContext _context;

    public ComplimentRepository(PortfolioDbContext context) => _context = context;

    public async Task<Compliment?> GetByIdAsync(int id)
        => await _context.Compliments.IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

    public async Task<List<Compliment>> GetByPortfolioAndCompanyAsync(int portfolioId, int companyId)
        => await _context.Compliments
            .Where(c => c.PortfolioId == portfolioId)
            .ToListAsync();

    public async Task<Compliment> CreateAsync(Compliment compliment)
    {
        _context.Compliments.Add(compliment);
        await _context.SaveChangesAsync();
        return compliment;
    }

    public async Task<Compliment> UpdateAsync(Compliment compliment)
    {
        _context.Compliments.Update(compliment);
        await _context.SaveChangesAsync();
        return compliment;
    }

    public async Task SoftDeleteAsync(int id)
    {
        await _context.Compliments.IgnoreQueryFilters()
            .Where(c => c.Id == id)
            .ExecuteUpdateAsync(s => s.SetProperty(c => c.IsDeleted, true));
    }
}

using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Interfaces;
using Portfolio.Domain.Entities;
using Portfolio.Infrastructure.Data;

namespace Portfolio.Infrastructure.Repositories;

public class CriterionRepository : ICriterionRepository
{
    private readonly PortfolioDbContext _context;

    public CriterionRepository(PortfolioDbContext context)
    {
        _context = context;
    }

    public async Task<List<Criterion>> GetAllAsync()
        => await _context.Criteria.OrderBy(x => x.Id).ToListAsync();

    public async Task<List<Criterion>> GetActiveAsync()
        => await _context.Criteria.Where(x => x.IsActive).OrderBy(x => x.Id).ToListAsync();

    public async Task<Criterion?> GetByIdAsync(int id)
        => await _context.Criteria.FindAsync(id);

    public async Task<Criterion> CreateAsync(Criterion criterion)
    {
        _context.Criteria.Add(criterion);
        await _context.SaveChangesAsync();
        return criterion;
    }

    public async Task<Criterion> UpdateAsync(Criterion criterion)
    {
        _context.Criteria.Update(criterion);
        await _context.SaveChangesAsync();
        return criterion;
    }
}

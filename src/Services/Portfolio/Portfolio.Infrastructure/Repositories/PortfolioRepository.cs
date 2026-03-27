using Microsoft.EntityFrameworkCore;
using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;
using Portfolio.Domain.Entities;
using Portfolio.Infrastructure.Data;

namespace Portfolio.Infrastructure.Repositories;

public class PortfolioRepository : IPortfolioRepository
{
    private readonly PortfolioDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public PortfolioRepository(PortfolioDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
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

    public async Task<(List<Portfolio.Domain.Entities.Portfolio> Items, int Total)> GetAllAsync(int page, int pageSize, string? status)
    {
        var query = _context.Portfolios.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(p => p.Status == status);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

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
        => await _context.BlockTypes.Where(x => x.IsActive).ToDictionaryAsync(x => x.Code);

    public async Task<(List<PortfolioWithComplimentDto> Items, int Total)> GetAllWithComplimentFilterAsync(PortfolioQueryParams queryParams)
    {
        var query = _context.Portfolios.AsQueryable();

        if (!string.IsNullOrWhiteSpace(queryParams.Status))
            query = query.Where(p => p.Status == queryParams.Status);

        if (queryParams.HasCompliment.HasValue)
        {
            if (queryParams.HasCompliment.Value)
                query = query.Where(p => _context.Compliments.IgnoreQueryFilters()
                    .Any(c => c.PortfolioId == p.Id && !c.IsDeleted
                           && (_currentUser.IsAdmin || c.CompanyId == _currentUser.CompanyId)
                           && (!queryParams.ComplimentState.HasValue || c.State == queryParams.ComplimentState)));
            else
                query = query.Where(p => !_context.Compliments.IgnoreQueryFilters()
                    .Any(c => c.PortfolioId == p.Id && !c.IsDeleted
                           && (_currentUser.IsAdmin || c.CompanyId == _currentUser.CompanyId)));
        }
        else if (queryParams.ComplimentState.HasValue)
        {
            query = query.Where(p => _context.Compliments.IgnoreQueryFilters()
                .Any(c => c.PortfolioId == p.Id && !c.IsDeleted && c.State == queryParams.ComplimentState
                       && (_currentUser.IsAdmin || c.CompanyId == _currentUser.CompanyId)));
        }

        var total = await query.CountAsync();

        var items = await query
            .OrderByDescending(p => p.CreatedAt)
            .Skip((queryParams.Page - 1) * queryParams.PageSize)
            .Take(queryParams.PageSize)
            .Select(p => new PortfolioWithComplimentDto
            {
                Id = p.Id,
                EmployeeId = p.EmployeeId,
                Name = p.Name,
                Status = p.Status,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                ComplimentCount = p.ComplimentCount,
                ApprovedComplimentCount = p.ApprovedComplimentCount,
                AverageScore = p.AverageScore,
                Compliments = !queryParams.IncludeCompliments ? null :
                    _context.Compliments.IgnoreQueryFilters()
                        .Where(c => c.PortfolioId == p.Id && !c.IsDeleted
                                 && (_currentUser.IsAdmin || c.CompanyId == _currentUser.CompanyId)
                                 && (!queryParams.ComplimentState.HasValue || c.State == queryParams.ComplimentState))
                        .Select(c => new ComplimentDto
                        {
                            Id = c.Id,
                            PortfolioId = c.PortfolioId,
                            CompanyId = c.CompanyId,
                            Content = c.Content,
                            Score = c.Score,
                            State = c.State,
                            CreatedAt = c.CreatedAt,
                            UpdatedAt = c.UpdatedAt
                        })
                        .ToList()
            })
            .ToListAsync();

        return (items, total);
    }
}

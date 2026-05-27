using Application.Application.Interfaces;
using Application.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Infrastructure.Repositories;

public class ApplicationRepository : IApplicationRepository
{
    private readonly ApplicationDbContext _context;

    public ApplicationRepository(ApplicationDbContext context) => _context = context;

    public async Task<Domain.Entities.Application?> GetByIdAsync(int id)
        => await _context.Applications.FirstOrDefaultAsync(a => a.ApplicationId == id);

    public async Task<List<Domain.Entities.Application>> GetByEmployeeIdAsync(int employeeId)
        => await _context.Applications.Where(a => a.EmployeeId == employeeId).OrderByDescending(a => a.AppliedAt).ToListAsync();

    public async Task<List<Domain.Entities.Application>> GetByCompanyIdAsync(int companyId)
        => await _context.Applications.Where(a => a.CompanyId == companyId).OrderByDescending(a => a.AppliedAt).ToListAsync();

    public async Task<int> CountByEmployeeIdAsync(int employeeId)
        => await _context.Applications.CountAsync(a => a.EmployeeId == employeeId);

    public async Task<(List<Domain.Entities.Application> Items, int Total)> GetByEmployeeIdPagedAsync(int employeeId, int page, int pageSize)
    {
        var query = _context.Applications
            .Where(a => a.EmployeeId == employeeId)
            .OrderByDescending(a => a.AppliedAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<(List<Domain.Entities.Application> Items, int Total)> GetByCompanyIdPagedAsync(int companyId, int page, int pageSize)
    {
        var query = _context.Applications
            .Where(a => a.CompanyId == companyId)
            .OrderByDescending(a => a.AppliedAt);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<Domain.Entities.Application> CreateAsync(Domain.Entities.Application application)
    {
        _context.Applications.Add(application);
        await _context.SaveChangesAsync();
        return application;
    }

    public async Task<Domain.Entities.Application> UpdateAsync(Domain.Entities.Application application)
    {
        _context.Applications.Update(application);
        await _context.SaveChangesAsync();
        return application;
    }

    public async Task<bool> ExistsByEmployeeAndPostAsync(int employeeId, int companyPostId)
        => await _context.Applications.AnyAsync(a => a.EmployeeId == employeeId && a.CompanyPostId == companyPostId);
}

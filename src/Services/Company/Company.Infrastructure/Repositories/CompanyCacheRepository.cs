using Company.Application.Interfaces;
using Company.Domain.Entities;
using Company.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Company.Infrastructure.Repositories;

public class CompanyCacheRepository : ICompanyCacheRepository
{
    private readonly CompanyDbContext _context;

    public CompanyCacheRepository(CompanyDbContext context)
    {
        _context = context;
    }

    public async Task<CompanyEntity?> GetByIdAsync(int companyId)
        => await _context.Companies.FirstOrDefaultAsync(c => c.Id == companyId);

    public async Task UpsertAsync(CompanyEntity company)
    {
        var existing = await _context.Companies.FirstOrDefaultAsync(c => c.Id == company.Id);
        if (existing == null)
        {
            _context.Companies.Add(company);
        }
        else
        {
            existing.Name = company.Name;
            existing.AvatarUrl = company.AvatarUrl;
        }

        await _context.SaveChangesAsync();
    }

    public async Task UpsertRangeAsync(IEnumerable<CompanyEntity> companies)
    {
        var companyList = companies.ToList();
        if (companyList.Count == 0) return;

        var ids = companyList.Select(c => c.Id).ToList();
        var existingCompanies = await _context.Companies
            .Where(c => ids.Contains(c.Id))
            .ToListAsync();

        var existingMap = existingCompanies.ToDictionary(c => c.Id);
        foreach (var company in companyList)
        {
            if (existingMap.TryGetValue(company.Id, out var existing))
            {
                existing.Name = company.Name;
                existing.AvatarUrl = company.AvatarUrl;
            }
            else
            {
                _context.Companies.Add(company);
            }
        }

        await _context.SaveChangesAsync();
    }
}

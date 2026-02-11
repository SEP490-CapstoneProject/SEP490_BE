using Microsoft.EntityFrameworkCore;
using UserProfile.Application.Interfaces;
using UserProfile.Domain.Entities;
using UserProfile.Infrastructure.Data;

namespace UserProfile.Infrastructure.Repositories;

public class CompanyRepository : ICompanyRepository
{
    private readonly UserProfileDbContext _context;

    public CompanyRepository(UserProfileDbContext context)
    {
        _context = context;
    }

    public async Task<Company?> GetByIdAsync(int id)
    {
        return await _context.Companies.FindAsync(id);
    }

    public async Task<Company?> GetByUserIdAsync(int userId)
    {
        return await _context.Companies.FirstOrDefaultAsync(c => c.UserId == userId);
    }

    public async Task<IEnumerable<Company>> GetAllAsync()
    {
        return await _context.Companies.ToListAsync();
    }

    public async Task<Company> CreateAsync(Company company)
    {
        _context.Companies.Add(company);
        await _context.SaveChangesAsync();
        return company;
    }

    public async Task<Company> UpdateAsync(Company company)
    {
        _context.Companies.Update(company);
        await _context.SaveChangesAsync();
        return company;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var company = await GetByIdAsync(id);
        if (company == null) return false;

        _context.Companies.Remove(company);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsByUserIdAsync(int userId)
    {
        return await _context.Companies.AnyAsync(c => c.UserId == userId);
    }
}

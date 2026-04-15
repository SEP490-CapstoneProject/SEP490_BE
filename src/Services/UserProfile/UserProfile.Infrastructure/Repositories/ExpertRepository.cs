using Microsoft.EntityFrameworkCore;
using UserProfile.Application.Interfaces;
using UserProfile.Domain.Entities;
using UserProfile.Infrastructure.Data;

namespace UserProfile.Infrastructure.Repositories;

public class ExpertRepository : IExpertRepository
{
    private readonly UserProfileDbContext _context;

    public ExpertRepository(UserProfileDbContext context)
    {
        _context = context;
    }

    public async Task<Expert?> GetByIdAsync(int id)
    {
        return await _context.Experts.FindAsync(id);
    }

    public async Task<Expert?> GetByUserIdAsync(int userId)
    {
        return await _context.Experts.FirstOrDefaultAsync(e => e.UserId == userId);
    }

    public async Task<IEnumerable<Expert>> GetAllAsync()
    {
        return await _context.Experts.ToListAsync();
    }

    public async Task<Expert> CreateAsync(Expert expert)
    {
        _context.Experts.Add(expert);
        await _context.SaveChangesAsync();
        return expert;
    }

    public async Task<Expert> UpdateAsync(Expert expert)
    {
        _context.Experts.Update(expert);
        await _context.SaveChangesAsync();
        return expert;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var expert = await GetByIdAsync(id);
        if (expert == null) return false;

        _context.Experts.Remove(expert);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsByUserIdAsync(int userId)
    {
        return await _context.Experts.AnyAsync(e => e.UserId == userId);
    }
}

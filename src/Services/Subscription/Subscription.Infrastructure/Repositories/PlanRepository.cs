using Microsoft.EntityFrameworkCore;
using Subscription.Application.Interfaces;
using Subscription.Domain.Entities;
using Subscription.Infrastructure.Data;

namespace Subscription.Infrastructure.Repositories;

public class PlanRepository : IPlanRepository
{
    private readonly SubscriptionDbContext _context;

    public PlanRepository(SubscriptionDbContext context)
    {
        _context = context;
    }

    public async Task<Plan?> GetByIdAsync(int id)
    {
        return await _context.Plans.FindAsync(id);
    }

    public async Task<Plan?> GetByIdWithFeaturesAsync(int id)
    {
        return await _context.Plans
            .Include(p => p.Features.Where(f => f.IsActive))
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IEnumerable<Plan>> GetAllActiveAsync()
    {
        return await _context.Plans
            .Include(p => p.Features.Where(f => f.IsActive))
            .Where(p => p.IsActive)
            .OrderBy(p => p.Price)
            .ToListAsync();
    }

    public async Task<IEnumerable<Plan>> GetActiveByRoleAsync(string role)
    {
        // Trả về plans có AllowedRole match với role, hoặc AllowedRole = null (all roles)
        return await _context.Plans
            .Include(p => p.Features.Where(f => f.IsActive))
            .Where(p => p.IsActive && (p.AllowedRole == null || p.AllowedRole == role))
            .OrderBy(p => p.Price)
            .ToListAsync();
    }

    public async Task<Plan?> GetFreePlanByRoleAsync(string role)
    {
        // Lấy plan có Price = 0 và AllowedRole khớp với role
        return await _context.Plans
            .Include(p => p.Features.Where(f => f.IsActive))
            .Where(p => p.IsActive && p.Price == 0 && p.AllowedRole == role)
            .FirstOrDefaultAsync();
    }

    public async Task<Plan> CreateAsync(Plan plan)
    {
        plan.CreatedAt = DateTime.UtcNow;
        _context.Plans.Add(plan);
        await _context.SaveChangesAsync();
        return plan;
    }

    public async Task UpdateAsync(Plan plan)
    {
        plan.UpdatedAt = DateTime.UtcNow;
        _context.Plans.Update(plan);
        await _context.SaveChangesAsync();
    }
}

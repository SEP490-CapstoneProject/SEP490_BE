using Microsoft.EntityFrameworkCore;
using UserProfile.Application.Interfaces;
using UserProfile.Domain.Entities;
using UserProfile.Infrastructure.Data;

namespace UserProfile.Infrastructure.Repositories;

public class EmployeeRepository : IEmployeeRepository
{
    private readonly UserProfileDbContext _context;

    public EmployeeRepository(UserProfileDbContext context)
    {
        _context = context;
    }

    public async Task<Employee?> GetByIdAsync(int id)
    {
        return await _context.Employees.FindAsync(id);
    }

    public async Task<Employee?> GetByUserIdAsync(int userId)
    {
        return await _context.Employees.FirstOrDefaultAsync(e => e.UserId == userId);
    }

    public async Task<IEnumerable<Employee>> GetAllAsync()
    {
        return await _context.Employees.ToListAsync();
    }

    public async Task<Employee> CreateAsync(Employee employee)
    {
        _context.Employees.Add(employee);
        await _context.SaveChangesAsync();
        return employee;
    }

    public async Task<Employee> UpdateAsync(Employee employee)
    {
        _context.Employees.Update(employee);
        await _context.SaveChangesAsync();
        return employee;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var employee = await GetByIdAsync(id);
        if (employee == null) return false;

        _context.Employees.Remove(employee);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ExistsByUserIdAsync(int userId)
    {
        return await _context.Employees.AnyAsync(e => e.UserId == userId);
    }
}

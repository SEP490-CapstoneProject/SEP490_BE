using Interview.Application.Interfaces;
using Interview.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using InterviewEntity = Interview.Domain.Entities.Interview;

namespace Interview.Infrastructure.Repositories;

public class InterviewRepository : IInterviewRepository
{
    private readonly InterviewDbContext _db;

    public InterviewRepository(InterviewDbContext db)
    {
        _db = db;
    }

    public async Task<InterviewEntity> CreateAsync(InterviewEntity interview)
    {
        interview.CreatedAt = DateTime.UtcNow;
        var ent = (await _db.Interviews.AddAsync(interview)).Entity;
        await _db.SaveChangesAsync();
        return ent;
    }

    public async Task DeleteAsync(int id)
    {
        var e = await _db.Interviews.FindAsync(id);
        if (e == null) return;
        _db.Interviews.Remove(e);
        await _db.SaveChangesAsync();
    }

    public async Task<IEnumerable<InterviewEntity>> GetAllAsync()
    {
        return await _db.Interviews.AsNoTracking().ToListAsync();
    }

    public async Task<IEnumerable<InterviewEntity>> GetAllByUserIdAsync(int userId)
    {
        return await _db.Interviews
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.Time)
            .ToListAsync();
    }

    public async Task<IEnumerable<InterviewEntity>> GetByDateAsync(int userId, DateTime date)
    {
        return await _db.Interviews
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Date.Date == date.Date)
            .OrderBy(x => x.Time)
            .ToListAsync();
    }

    public async Task<IEnumerable<InterviewEntity>> GetByStatusAsync(int userId, string status)
    {
        return await _db.Interviews
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.Status == status)
            .OrderByDescending(x => x.Date)
            .ThenByDescending(x => x.Time)
            .ToListAsync();
    }

    public async Task<InterviewEntity?> GetByIdAsync(int id)
    {
        return await _db.Interviews.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
    }

    public async Task UpdateAsync(InterviewEntity interview)
    {
        interview.UpdatedAt = DateTime.UtcNow;
        _db.Interviews.Update(interview);
        await _db.SaveChangesAsync();
    }
}


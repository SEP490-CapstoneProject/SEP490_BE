using Microsoft.EntityFrameworkCore;
using Challenge.Domain.Entities;
using Challenge.Domain.Repositories;

namespace Challenge.Infrastructure.Persistence.Repositories;

public class SkillRepository : ISkillRepository
{
    private readonly ChallengeDbContext _context;

    public SkillRepository(ChallengeDbContext context)
    {
        _context = context;
    }

    public async Task<Skill> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Skills.FindAsync(new object[] { id }, cancellationToken: cancellationToken);

    public async Task<Skill> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        => await _context.Skills.FirstOrDefaultAsync(s => s.Name == name, cancellationToken);

    public async Task<Skill> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
        => await _context.Skills.FirstOrDefaultAsync(s => s.Slug == slug, cancellationToken);

    public async Task<IEnumerable<Skill>> GetAllAsync(CancellationToken cancellationToken = default)
        => await _context.Skills.OrderBy(s => s.Name).ToListAsync(cancellationToken);

    public Task<IEnumerable<Skill>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
        => Task.FromResult<IEnumerable<Skill>>(new List<Skill>());

    public Task<IEnumerable<SkillAlias>> GetAliasesBySkillAsync(Guid skillId, CancellationToken cancellationToken = default)
        => Task.FromResult<IEnumerable<SkillAlias>>(new List<SkillAlias>());

    public Task<Skill> GetByAliasAsync(string alias, CancellationToken cancellationToken = default)
        => Task.FromResult<Skill?>(null)!;

    public Task<IEnumerable<Skill>> FuzzySearchAsync(string searchTerm, decimal threshold = 0.85m, CancellationToken cancellationToken = default)
        => Task.FromResult<IEnumerable<Skill>>(new List<Skill>());

    public async Task AddAsync(Skill skill, CancellationToken cancellationToken = default)
    {
        _context.Skills.Add(skill);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task AddAliasAsync(SkillAlias alias, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public async Task UpdateAsync(Skill skill, CancellationToken cancellationToken = default)
    {
        _context.Skills.Update(skill);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Skills.AnyAsync(s => s.Id == id, cancellationToken);
}

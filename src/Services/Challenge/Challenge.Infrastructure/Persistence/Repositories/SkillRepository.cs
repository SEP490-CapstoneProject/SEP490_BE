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

    public async Task<IEnumerable<Skill>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
        => await _context.Skills.Where(s => s.CategoryId == categoryId).OrderBy(s => s.Name).ToListAsync(cancellationToken);

    public async Task<IEnumerable<SkillAlias>> GetAliasesBySkillAsync(Guid skillId, CancellationToken cancellationToken = default)
        => await _context.SkillAliases.Where(a => a.SkillId == skillId).OrderBy(a => a.Alias).ToListAsync(cancellationToken);

    public async Task<Skill> GetByAliasAsync(string alias, CancellationToken cancellationToken = default)
    {
        var skillId = await _context.SkillAliases
            .Where(a => a.Alias == alias)
            .Select(a => a.SkillId)
            .FirstOrDefaultAsync(cancellationToken);

        return skillId == Guid.Empty
            ? null
            : await GetByIdAsync(skillId, cancellationToken);
    }

    public async Task<IEnumerable<Skill>> FuzzySearchAsync(string searchTerm, decimal threshold = 0.85m, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return Array.Empty<Skill>();
        }

        var normalized = searchTerm.Trim().ToLowerInvariant();

        var byName = await _context.Skills
            .Where(s => s.Name.ToLower().Contains(normalized) || s.Slug.ToLower().Contains(normalized))
            .ToListAsync(cancellationToken);

        var aliasSkillIds = await _context.SkillAliases
            .Where(a => a.Alias.ToLower().Contains(normalized))
            .Select(a => a.SkillId)
            .ToListAsync(cancellationToken);

        var byAlias = await _context.Skills
            .Where(s => aliasSkillIds.Contains(s.Id))
            .ToListAsync(cancellationToken);

        return byName
            .Concat(byAlias)
            .GroupBy(s => s.Id)
            .Select(g => g.First())
            .OrderBy(s => s.Name)
            .ToList();
    }

    public async Task AddAsync(Skill skill, CancellationToken cancellationToken = default)
    {
        _context.Skills.Add(skill);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddAliasAsync(SkillAlias alias, CancellationToken cancellationToken = default)
    {
        _context.SkillAliases.Add(alias);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Skill skill, CancellationToken cancellationToken = default)
    {
        _context.Skills.Update(skill);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var skill = await GetByIdAsync(id, cancellationToken);
        if (skill != null)
        {
            _context.Skills.Remove(skill);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default)
        => await _context.Skills.AnyAsync(s => s.Id == id, cancellationToken);
}

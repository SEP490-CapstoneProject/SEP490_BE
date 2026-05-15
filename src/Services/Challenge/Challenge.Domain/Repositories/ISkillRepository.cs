using Challenge.Domain.Entities;
namespace Challenge.Domain.Repositories;

public interface ISkillRepository
{
    Task<Skill> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Skill> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<Skill> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<IEnumerable<Skill>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Skill>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SkillAlias>> GetAliasesBySkillAsync(Guid skillId, CancellationToken cancellationToken = default);
    Task<Skill> GetByAliasAsync(string alias, CancellationToken cancellationToken = default);
    Task<IEnumerable<Skill>> FuzzySearchAsync(string searchTerm, decimal threshold = 0.85m, CancellationToken cancellationToken = default);
    
    Task AddAsync(Skill skill, CancellationToken cancellationToken = default);
    Task AddAliasAsync(SkillAlias alias, CancellationToken cancellationToken = default);
    Task UpdateAsync(Skill skill, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
}

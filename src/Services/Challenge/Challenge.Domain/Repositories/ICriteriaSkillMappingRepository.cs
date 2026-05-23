using Challenge.Domain.Entities;

namespace Challenge.Domain.Repositories;

public interface ICriteriaSkillMappingRepository
{
    Task AddRangeAsync(IEnumerable<CriteriaSkillMapping> mappings, CancellationToken cancellationToken = default);
}

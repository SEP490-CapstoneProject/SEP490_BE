using Challenge.Domain.Entities;
namespace Challenge.Domain.Repositories;

public interface IUserSkillRepository
{
    Task<UserSkill> GetByUserAndSkillAsync(Guid userId, Guid skillId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserSkill>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserSkill>> GetVerifiedByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserSkill>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<decimal> GetTotalPointsByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    
    Task AddAsync(UserSkill userSkill, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserSkill userSkill, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid userId, Guid skillId, CancellationToken cancellationToken = default);
}

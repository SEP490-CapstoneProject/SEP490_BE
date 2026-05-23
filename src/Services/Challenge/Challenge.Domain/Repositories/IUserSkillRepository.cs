using Challenge.Domain.Entities;
namespace Challenge.Domain.Repositories;

public interface IUserSkillRepository
{
    Task<UserSkill> GetByUserAndSkillAsync(int userId, Guid skillId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserSkill>> GetByUserAsync(int userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserSkill>> GetVerifiedByUserAsync(int userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<UserSkill>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<decimal> GetTotalPointsByUserAsync(int userId, CancellationToken cancellationToken = default);
    
    Task AddAsync(UserSkill userSkill, CancellationToken cancellationToken = default);
    Task UpdateAsync(UserSkill userSkill, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(int userId, Guid skillId, CancellationToken cancellationToken = default);
}

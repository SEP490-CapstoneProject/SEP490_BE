using Challenge.Domain.Entities;
namespace Challenge.Domain.Repositories;

public interface ISkillPointTransactionRepository
{
    Task<SkillPointTransaction> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<SkillPointTransaction>> GetByUserAsync(int userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SkillPointTransaction>> GetByUserAndSkillAsync(int userId, Guid skillId, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalPointsByUserAndSkillAsync(int userId, Guid skillId, CancellationToken cancellationToken = default);
    
    Task AddAsync(SkillPointTransaction transaction, CancellationToken cancellationToken = default);
}

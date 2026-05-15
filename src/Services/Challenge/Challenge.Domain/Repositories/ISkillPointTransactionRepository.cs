using Challenge.Domain.Entities;
namespace Challenge.Domain.Repositories;

public interface ISkillPointTransactionRepository
{
    Task<SkillPointTransaction> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<SkillPointTransaction>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<SkillPointTransaction>> GetByUserAndSkillAsync(Guid userId, Guid skillId, CancellationToken cancellationToken = default);
    Task<decimal> GetTotalPointsByUserAndSkillAsync(Guid userId, Guid skillId, CancellationToken cancellationToken = default);
    
    Task AddAsync(SkillPointTransaction transaction, CancellationToken cancellationToken = default);
}

using Portfolio.Domain.Entities;

namespace Portfolio.Application.Interfaces;

public interface IComplimentRepository
{
    Task<Compliment?> GetByIdAsync(int id);
    Task<List<Compliment>> GetByPortfolioAndUserAsync(int portfolioId, int userId, bool isAdmin);
    Task<Compliment> CreateAsync(Compliment compliment);
    Task<Compliment> UpdateAsync(Compliment compliment);
    Task MarkDeletedAsync(int id, int updatedBy);
    
    /// <summary>
    /// Checks if creator has any non-deleted compliment on this portfolio.
    /// </summary>
    Task<bool> HasComplimentFromCreatorAsync(int portfolioId, int createdBy, int? excludeComplimentId = null);
    
    /// <summary>
    /// Checks if creator has any non-deleted compliment on this portfolio within last N days.
    /// </summary>
    Task<bool> HasComplimentFromCreatorInLastDaysAsync(int portfolioId, int createdBy, int days, int? excludeComplimentId = null);
}

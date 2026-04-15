using Portfolio.Domain.Entities;

namespace Portfolio.Application.Interfaces;

public interface IComplimentRepository
{
    Task<Compliment?> GetByIdAsync(int id);
    Task<List<Compliment>> GetByPortfolioAndUserAsync(int portfolioId, int userId, bool isAdmin);
    Task<Compliment> CreateAsync(Compliment compliment);
    Task<Compliment> UpdateAsync(Compliment compliment);
    Task MarkDeletedAsync(int id, int updatedBy);
}

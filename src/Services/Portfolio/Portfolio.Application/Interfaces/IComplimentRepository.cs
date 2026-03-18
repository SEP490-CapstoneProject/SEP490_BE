using Portfolio.Domain.Entities;

namespace Portfolio.Application.Interfaces;

public interface IComplimentRepository
{
    Task<Compliment?> GetByIdAsync(int id);
    Task<List<Compliment>> GetByPortfolioAndCompanyAsync(int portfolioId, int companyId);
    Task<Compliment> CreateAsync(Compliment compliment);
    Task<Compliment> UpdateAsync(Compliment compliment);
    Task SoftDeleteAsync(int id);
}

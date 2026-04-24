using Portfolio.Domain.Entities;

namespace Portfolio.Application.Interfaces;

public interface ICriterionRepository
{
    Task<List<Criterion>> GetAllAsync();
    Task<List<Criterion>> GetActiveAsync();
    Task<Criterion?> GetByIdAsync(int id);
    Task<Criterion> CreateAsync(Criterion criterion);
    Task<Criterion> UpdateAsync(Criterion criterion);
}

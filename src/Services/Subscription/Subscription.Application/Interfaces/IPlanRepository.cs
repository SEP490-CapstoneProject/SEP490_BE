using Subscription.Domain.Entities;

namespace Subscription.Application.Interfaces;

public interface IPlanRepository
{
    Task<Plan?> GetByIdAsync(int id);
    Task<Plan?> GetByIdWithFeaturesAsync(int id);
    Task<IEnumerable<Plan>> GetAllActiveAsync();
    Task<Plan> CreateAsync(Plan plan);
    Task UpdateAsync(Plan plan);
}

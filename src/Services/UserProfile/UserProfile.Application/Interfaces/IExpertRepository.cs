using UserProfile.Domain.Entities;

namespace UserProfile.Application.Interfaces;

public interface IExpertRepository
{
    Task<Expert?> GetByIdAsync(int id);
    Task<Expert?> GetByUserIdAsync(int userId);
    Task<IEnumerable<Expert>> GetAllAsync();
    Task<Expert> CreateAsync(Expert expert);
    Task<Expert> UpdateAsync(Expert expert);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsByUserIdAsync(int userId);
}

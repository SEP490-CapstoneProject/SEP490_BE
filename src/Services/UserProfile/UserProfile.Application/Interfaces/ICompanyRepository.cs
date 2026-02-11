using UserProfile.Domain.Entities;

namespace UserProfile.Application.Interfaces;

public interface ICompanyRepository
{
    Task<Company?> GetByIdAsync(int id);
    Task<Company?> GetByUserIdAsync(int userId);
    Task<IEnumerable<Company>> GetAllAsync();
    Task<Company> CreateAsync(Company company);
    Task<Company> UpdateAsync(Company company);
    Task<bool> DeleteAsync(int id);
    Task<bool> ExistsByUserIdAsync(int userId);
}

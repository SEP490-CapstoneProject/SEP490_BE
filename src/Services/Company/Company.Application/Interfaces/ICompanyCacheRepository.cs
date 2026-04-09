using Company.Domain.Entities;

namespace Company.Application.Interfaces;

public interface ICompanyCacheRepository
{
    Task<CompanyEntity?> GetByIdAsync(int companyId);
    Task UpsertAsync(CompanyEntity company);
    Task UpsertRangeAsync(IEnumerable<CompanyEntity> companies);
}

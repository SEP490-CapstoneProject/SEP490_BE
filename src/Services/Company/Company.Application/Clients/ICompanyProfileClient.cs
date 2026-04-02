using Company.Application.DTOs;

namespace Company.Application.Clients;

public interface ICompanyProfileClient
{
    Task<CompanyProfileDto?> GetCompanyAsync(int companyId);
    Task<IReadOnlyList<CompanyProfileDto>> GetCompaniesAsync(IEnumerable<int> companyIds);
}

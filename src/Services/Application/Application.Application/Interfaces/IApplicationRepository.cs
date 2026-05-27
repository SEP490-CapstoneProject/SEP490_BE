namespace Application.Application.Interfaces;

public interface IApplicationRepository
{
    Task<Domain.Entities.Application?> GetByIdAsync(int id);
    Task<List<Domain.Entities.Application>> GetByEmployeeIdAsync(int employeeId);
    Task<List<Domain.Entities.Application>> GetByCompanyIdAsync(int companyId);
    Task<int> CountByEmployeeIdAsync(int employeeId);
    Task<(List<Domain.Entities.Application> Items, int Total)> GetByEmployeeIdPagedAsync(int employeeId, int page, int pageSize);
    Task<(List<Domain.Entities.Application> Items, int Total)> GetByCompanyIdPagedAsync(int companyId, int page, int pageSize);
    Task<Domain.Entities.Application> CreateAsync(Domain.Entities.Application application);
    Task<Domain.Entities.Application> UpdateAsync(Domain.Entities.Application application);
    Task<bool> ExistsByEmployeeAndPostAsync(int employeeId, int companyPostId);
}

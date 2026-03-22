using Application.Application.DTOs;

namespace Application.Application.Interfaces;

public interface IApplicationService
{
    Task<ApplicationDto> CreateApplicationAsync(CreateApplicationRequest request);
    Task<PagedResult<ApplicationDto>> GetMyApplicationsAsync(int page, int pageSize);
    Task<PagedResult<ApplicationManagerDto>> GetCompanyApplicationsAsync(int page, int pageSize);
    Task<ApplicationDto> UpdateApplicationStatusAsync(int id, UpdateApplicationStatusRequest request);
    Task<ApplicationDto> GetApplicationByIdAsync(int id);
}

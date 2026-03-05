using Portfolio.Domain.Entities;

namespace Portfolio.Application.Interfaces;

public interface IBlockRepository
{
    Task<PortfolioBlock?> GetByIdWithDataAsync(int blockId);
    Task<List<PortfolioBlock>> GetByPortfolioIdAsync(int portfolioId);
    Task<int> GetMaxOrderAsync(int portfolioId);
    Task<int> CountByTypeAsync(int portfolioId, int blockTypeId);
    Task<PortfolioBlock> CreateAsync(PortfolioBlock block);
    Task<PortfolioBlock> UpdateAsync(PortfolioBlock block);
    Task DeleteAsync(PortfolioBlock block);
    Task ReorderAsync(List<(int blockId, int order)> reorders);

    // Data table add/update/delete helpers
    Task AddIntroAsync(Intro intro);
    Task UpdateIntroAsync(Intro intro);
    Task AddSkillsAsync(IEnumerable<Skill> skills);
    Task RemoveSkillsAsync(int portfolioBlockId);
    Task AddEducationAsync(Education education);
    Task UpdateEducationAsync(Education education);
    Task AddDiplomaAsync(Diploma diploma);
    Task UpdateDiplomaAsync(Diploma diploma);
    Task AddExperienceAsync(Experience experience);
    Task UpdateExperienceAsync(Experience experience);
    Task AddProjectAsync(Project project);
    Task UpdateProjectAsync(Project project);
    Task AddAwardAsync(Award award);
    Task UpdateAwardAsync(Award award);
    Task AddActivitiesAsync(Activities activities);
    Task UpdateActivitiesAsync(Activities activities);
    Task AddOtherInfoAsync(OtherInfo otherInfo);
    Task UpdateOtherInfoAsync(OtherInfo otherInfo);
    Task AddReferenceAsync(Reference reference);
    Task UpdateReferenceAsync(Reference reference);
}

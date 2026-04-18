using Portfolio.Application.DTOs;

namespace Portfolio.Application.Interfaces;

public interface IPortfolioFollowCategoryService
{
    Task<FollowCategoryDto> CreateAsync(CreateFollowCategoryRequest request);
    Task<List<FollowCategoryDto>> GetMyCategoriesAsync();
    Task<FollowCategoryDto> UpdateAsync(int categoryId, UpdateFollowCategoryRequest request);
    Task DeleteAsync(int categoryId);
}

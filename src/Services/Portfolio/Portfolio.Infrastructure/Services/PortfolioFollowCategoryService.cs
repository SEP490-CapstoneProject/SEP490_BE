using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;
using Portfolio.Domain.Entities;
using RecruitmentPlatform.Contracts.Time;
using System.Text;

namespace Portfolio.Infrastructure.Services;

public class PortfolioFollowCategoryService : IPortfolioFollowCategoryService
{
    private readonly IPortfolioFollowCategoryRepository _categoryRepository;
    private readonly ICurrentUserService _currentUser;

    public PortfolioFollowCategoryService(
        IPortfolioFollowCategoryRepository categoryRepository,
        ICurrentUserService currentUser)
    {
        _categoryRepository = categoryRepository;
        _currentUser = currentUser;
    }

    public async Task<FollowCategoryDto> CreateAsync(CreateFollowCategoryRequest request)
    {
        EnsureCompanyAccess();
        var name = NormalizeName(request.Name);
        var code = await BuildUniqueCodeAsync(name, null);

        var category = new PortfolioFollowCategory
        {
            CompanyId = _currentUser.CompanyId,
            Name = name,
            Code = code,
            CreatedAt = VietnamTime.Now()
        };

        var created = await _categoryRepository.CreateAsync(category);
        return MapToDto(created);
    }

    public async Task<List<FollowCategoryDto>> GetMyCategoriesAsync()
    {
        EnsureCompanyAccess();
        var categories = await _categoryRepository.GetByCompanyAsync(_currentUser.CompanyId);
        return categories.Select(MapToDto).ToList();
    }

    public async Task<FollowCategoryDto> UpdateAsync(int categoryId, UpdateFollowCategoryRequest request)
    {
        EnsureCompanyAccess();

        var category = await _categoryRepository.GetByIdAsync(categoryId)
            ?? throw new KeyNotFoundException($"Category {categoryId} not found");

        if (category.CompanyId != _currentUser.CompanyId)
        {
            throw new UnauthorizedAccessException("Category does not belong to this company.");
        }

        var name = NormalizeName(request.Name);
        category.Name = name;
        category.Code = await BuildUniqueCodeAsync(name, categoryId);
        category.UpdatedAt = VietnamTime.Now();

        var updated = await _categoryRepository.UpdateAsync(category);
        return MapToDto(updated);
    }

    public async Task DeleteAsync(int categoryId)
    {
        EnsureCompanyAccess();

        var category = await _categoryRepository.GetByIdAsync(categoryId)
            ?? throw new KeyNotFoundException($"Category {categoryId} not found");

        if (category.CompanyId != _currentUser.CompanyId)
        {
            throw new UnauthorizedAccessException("Category does not belong to this company.");
        }

        if (await _categoryRepository.IsCategoryInUseAsync(categoryId))
        {
            throw new InvalidOperationException("Category is being used by followed portfolios and cannot be deleted.");
        }

        await _categoryRepository.DeleteAsync(category);
    }

    private void EnsureCompanyAccess()
    {
        if (!_currentUser.HasCompany)
        {
            throw new UnauthorizedAccessException("Company identity required.");
        }
    }

    private static string NormalizeName(string? name)
    {
        var normalized = name?.Trim();
        if (string.IsNullOrWhiteSpace(normalized))
        {
            throw new ArgumentException("Category name is required.");
        }

        if (normalized.Length > 100)
        {
            throw new ArgumentException("Category name must be 100 characters or fewer.");
        }

        return normalized;
    }

    private async Task<string> BuildUniqueCodeAsync(string name, int? ignoreCategoryId)
    {
        var baseCode = ToSlug(name);
        if (string.IsNullOrWhiteSpace(baseCode))
        {
            baseCode = "category";
        }

        var code = baseCode;
        var suffix = 2;
        while (true)
        {
            var existing = await _categoryRepository.GetByCompanyAndCodeAsync(_currentUser.CompanyId, code);
            if (existing == null || (ignoreCategoryId.HasValue && existing.Id == ignoreCategoryId.Value))
            {
                return code;
            }

            code = $"{baseCode}-{suffix}";
            suffix++;
        }
    }

    private static string ToSlug(string input)
    {
        var builder = new StringBuilder(input.Length);
        var previousDash = false;

        foreach (var ch in input.ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
                previousDash = false;
            }
            else if (!previousDash)
            {
                builder.Append('-');
                previousDash = true;
            }
        }

        return builder.ToString().Trim('-');
    }

    private static FollowCategoryDto MapToDto(PortfolioFollowCategory category) => new()
    {
        Id = category.Id,
        Name = category.Name,
        Code = category.Code,
        CreatedAt = category.CreatedAt,
        UpdatedAt = category.UpdatedAt
    };
}

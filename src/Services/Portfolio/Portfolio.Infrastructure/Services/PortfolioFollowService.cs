using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;
using Portfolio.Domain.Entities;
using RecruitmentPlatform.Contracts.Time;

namespace Portfolio.Infrastructure.Services;

public class PortfolioFollowService : IPortfolioFollowService
{
    private static readonly HashSet<string> ValidInterestLevels = new(StringComparer.OrdinalIgnoreCase)
    {
        "low", "medium", "high"
    };

    private readonly IPortfolioFollowRepository _followRepository;
    private readonly IPortfolioFollowCategoryRepository _categoryRepository;
    private readonly IPortfolioRepository _portfolioRepository;
    private readonly IBlockRepository _blockRepository;
    private readonly IBlockService _blockService;
    private readonly ICurrentUserService _currentUser;

    public PortfolioFollowService(
        IPortfolioFollowRepository followRepository,
        IPortfolioFollowCategoryRepository categoryRepository,
        IPortfolioRepository portfolioRepository,
        IBlockRepository blockRepository,
        IBlockService blockService,
        ICurrentUserService currentUser)
    {
        _followRepository = followRepository;
        _categoryRepository = categoryRepository;
        _portfolioRepository = portfolioRepository;
        _blockRepository = blockRepository;
        _blockService = blockService;
        _currentUser = currentUser;
    }

    public async Task<PortfolioFollowDto> CreateAsync(CreatePortfolioFollowRequest request)
    {
        EnsureCompanyAccess();

        var interestLevel = NormalizeInterestLevel(request.InterestLevel);
        var portfolio = await _portfolioRepository.GetByIdAsync(request.PortfolioId)
            ?? throw new KeyNotFoundException($"Portfolio {request.PortfolioId} not found");

        var existing = await _followRepository.GetByCompanyAndPortfolioAsync(_currentUser.CompanyId, request.PortfolioId);
        if (existing != null)
            throw new InvalidOperationException($"Portfolio {request.PortfolioId} is already followed by this company.");

        var follow = new PortfolioFollow
        {
            CompanyId = _currentUser.CompanyId,
            PortfolioId = request.PortfolioId,
            CategoryId = await ResolveCategoryIdAsync(request.CategoryId),
            InterestLevel = interestLevel,
            FollowedAt = VietnamTime.Now()
        };

        var created = await _followRepository.CreateAsync(follow);
        created.Portfolio = portfolio;
        return await MapToDtoAsync(created);
    }

    public async Task<List<PortfolioFollowDto>> GetMyFollowsAsync(int? categoryId = null)
    {
        EnsureCompanyAccess();

        var validatedCategoryId = await ValidateCategoryFilterAsync(categoryId);
        var follows = await _followRepository.GetByCompanyAsync(_currentUser.CompanyId, validatedCategoryId);
        var result = new List<PortfolioFollowDto>(follows.Count);
        foreach (var follow in follows)
        {
            result.Add(await MapToDtoAsync(follow));
        }

        return result;
    }

    public async Task<PortfolioFollowDto> UpdateInterestAsync(int portfolioId, UpdatePortfolioFollowRequest request)
    {
        EnsureCompanyAccess();

        var follow = await _followRepository.GetByCompanyAndPortfolioAsync(_currentUser.CompanyId, portfolioId)
            ?? throw new KeyNotFoundException($"Follow not found for portfolio {portfolioId}");

        follow.InterestLevel = NormalizeInterestLevel(request.InterestLevel);
        if (request.CategoryId.HasValue)
        {
            follow.CategoryId = await ResolveCategoryIdAsync(request.CategoryId);
        }
        follow.UpdatedAt = VietnamTime.Now();
        var updated = await _followRepository.UpdateAsync(follow);
        return await MapToDtoAsync(updated);
    }

    public async Task DeleteAsync(int portfolioId)
    {
        EnsureCompanyAccess();

        var follow = await _followRepository.GetByCompanyAndPortfolioAsync(_currentUser.CompanyId, portfolioId)
            ?? throw new KeyNotFoundException($"Follow not found for portfolio {portfolioId}");

        await _followRepository.DeleteAsync(follow);
    }

    private void EnsureCompanyAccess()
    {
        if (!_currentUser.HasCompany)
        {
            throw new UnauthorizedAccessException("Company identity required.");
        }
    }

    private static string NormalizeInterestLevel(string? interestLevel)
    {
        var normalized = interestLevel?.Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(normalized) || !ValidInterestLevels.Contains(normalized))
        {
            throw new ArgumentException("InterestLevel must be one of: low, medium, high.");
        }

        return normalized;
    }

    private async Task<int?> ResolveCategoryIdAsync(int? categoryId)
    {
        if (!categoryId.HasValue)
        {
            return null;
        }

        if (categoryId.Value <= 0)
        {
            throw new ArgumentException("CategoryId must be greater than 0.");
        }

        var category = await _categoryRepository.GetByIdAsync(categoryId.Value)
            ?? throw new KeyNotFoundException($"Category {categoryId.Value} not found");

        if (category.CompanyId != _currentUser.CompanyId)
        {
            throw new UnauthorizedAccessException("Category does not belong to this company.");
        }

        return category.Id;
    }

    private async Task<int?> ValidateCategoryFilterAsync(int? categoryId)
    {
        if (!categoryId.HasValue)
        {
            return null;
        }

        if (categoryId.Value <= 0)
        {
            throw new ArgumentException("CategoryId must be greater than 0.");
        }

        var category = await _categoryRepository.GetByIdAsync(categoryId.Value);
        if (category == null || category.CompanyId != _currentUser.CompanyId)
        {
            throw new ArgumentException("Category is invalid for this company.");
        }

        return category.Id;
    }

    private async Task<PortfolioFollowDto> MapToDtoAsync(PortfolioFollow follow)
    {
        var portfolio = follow.Portfolio ?? await _portfolioRepository.GetByIdAsync(follow.PortfolioId)
            ?? throw new KeyNotFoundException($"Portfolio {follow.PortfolioId} not found");

        var lastPortfolioUpdate = portfolio.UpdatedAt ?? portfolio.CreatedAt;
        var blocks = await _blockRepository.GetByPortfolioIdAsync(portfolio.Id);
        var firstBlock = blocks.OrderBy(b => b.DisplayOrder).FirstOrDefault();

        PortfolioPreviewDto? preview = null;
        if (firstBlock != null)
        {
            var blockDto = _blockService.MapBlockToDto(firstBlock);
            preview = new PortfolioPreviewDto
            {
                BlockId = firstBlock.Id,
                Type = blockDto.Type,
                Variant = blockDto.Variant,
                Data = blockDto.Data
            };
        }

        return new PortfolioFollowDto
        {
            PortfolioId = portfolio.Id,
            EmployeeId = portfolio.EmployeeId,
            PortfolioName = portfolio.Name,
            Status = portfolio.Status,
            InterestLevel = follow.InterestLevel,
            CategoryId = follow.CategoryId,
            CategoryName = follow.Category?.Name,
            CategoryCode = follow.Category?.Code,
            FollowedAt = follow.FollowedAt,
            LastPortfolioUpdateAt = lastPortfolioUpdate,
            IsUpdatedSinceFollow = lastPortfolioUpdate > follow.FollowedAt,
            Preview = preview
        };
    }
}

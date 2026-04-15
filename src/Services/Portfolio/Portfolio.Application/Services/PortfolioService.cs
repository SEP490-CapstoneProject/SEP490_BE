using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Portfolio.Application.BlockHandlers;
using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;
using Portfolio.Domain.Entities;
using RecruitmentPlatform.Contracts.Time;
using System.Transactions;

namespace Portfolio.Application.Services;

public class PortfolioService : IPortfolioService
{
    private readonly IPortfolioRepository _repo;
    private readonly IEmployeeServiceClient _employeeClient;
    private readonly IAuthServiceClient _authServiceClient;
    private readonly IReviewerProfileClient _reviewerProfileClient;
    private readonly IEnumerable<IBlockHandler> _handlers;
    private readonly ILogger<PortfolioService> _logger;

    public PortfolioService(
        IPortfolioRepository repo,
        IEmployeeServiceClient employeeClient,
        IAuthServiceClient authServiceClient,
        IReviewerProfileClient reviewerProfileClient,
        IEnumerable<IBlockHandler> handlers,
        ILogger<PortfolioService> logger)
    {
        _repo = repo;
        _employeeClient = employeeClient;
        _authServiceClient = authServiceClient;
        _reviewerProfileClient = reviewerProfileClient;
        _handlers = handlers;
        _logger = logger;
    }

    public async Task<PortfolioDto?> GetByIdAsync(int id)
    {
        var p = await _repo.GetByIdAsync(id);
        return p == null ? null : MapToDto(p);
    }

    public async Task<IEnumerable<PortfolioDto>> GetByEmployeeIdAsync(int employeeId)
    {
        var list = await _repo.GetByEmployeeIdAsync(employeeId);
        return list.Select(MapToDto);
    }

    public async Task<PortfolioDto?> GetMainByEmployeeIdAsync(int employeeId)
    {
        var portfolio = await _repo.GetMainByEmployeeIdAsync(employeeId);
        return portfolio == null ? null : MapToDto(portfolio);
    }

    public async Task<PortfolioDto> CreateAsync(int employeeId, CreatePortfolioRequest request)
    {
        var valid = await _employeeClient.ValidateEmployeeAsync(employeeId);
        if (!valid)
            throw new KeyNotFoundException($"Employee {employeeId} not found");

        var portfolio = new Domain.Entities.Portfolio
        {
            EmployeeId = employeeId,
            Name = request.Name,
            Status = "active",
            IsMain = request.IsMain,
            IsPublic = request.IsPublic,
            CreatedAt = VietnamTime.Now()
        };

        var created = await _repo.CreateAsync(portfolio);
        if (request.IsMain)
        {
            await _repo.SetMainPortfolioAsync(employeeId, created.Id);
            created.IsMain = true;
        }
        _logger.LogInformation("Portfolio {Id} created for employee {EmpId}", created.Id, employeeId);
        return MapToDto(created);
    }

    public async Task<PortfolioDto> UpdateAsync(int id, int employeeId, UpdatePortfolioRequest request)
    {
        var portfolio = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Portfolio {id} not found");

        if (portfolio.EmployeeId != employeeId)
            throw new UnauthorizedAccessException("You do not own this portfolio");

        portfolio.Name = request.Name;
        portfolio.Status = request.Status;
        if (request.IsPublic.HasValue)
            portfolio.IsPublic = request.IsPublic.Value;
        if (request.IsMain.HasValue)
            portfolio.IsMain = request.IsMain.Value;
        portfolio.UpdatedAt = VietnamTime.Now();

        if (request.IsMain == true)
            await _repo.SetMainPortfolioAsync(employeeId, portfolio.Id);

        var updated = await _repo.UpdateAsync(portfolio);
        return MapToDto(updated);
    }

    public async Task<PortfolioDto> ToggleMainAsync(int id, int employeeId)
    {
        var portfolio = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Portfolio {id} not found");

        if (portfolio.EmployeeId != employeeId)
            throw new UnauthorizedAccessException("You do not own this portfolio");

        if (portfolio.IsMain)
        {
            portfolio.IsMain = false;
            portfolio.UpdatedAt = VietnamTime.Now();
            var updated = await _repo.UpdateAsync(portfolio);
            return MapToDto(updated);
        }

        await _repo.SetMainPortfolioAsync(employeeId, portfolio.Id);
        portfolio.IsMain = true;
        portfolio.UpdatedAt = VietnamTime.Now();
        var saved = await _repo.UpdateAsync(portfolio);
        return MapToDto(saved);
    }

    public async Task<PortfolioDto> TogglePublicAsync(int id, int employeeId)
    {
        var portfolio = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Portfolio {id} not found");

        if (portfolio.EmployeeId != employeeId)
            throw new UnauthorizedAccessException("You do not own this portfolio");

        portfolio.IsPublic = !portfolio.IsPublic;
        portfolio.UpdatedAt = VietnamTime.Now();

        var updated = await _repo.UpdateAsync(portfolio);
        return MapToDto(updated);
    }

    public async Task<bool> DeleteAsync(int id, int employeeId)
    {
        var portfolio = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Portfolio {id} not found");

        if (portfolio.EmployeeId != employeeId)
            throw new UnauthorizedAccessException("You do not own this portfolio");

        return await _repo.DeleteAsync(id);
    }

    public async Task<PagedResult<PortfolioDto>> GetAllAsync(int page, int pageSize, string? status, PortfolioSortMode sort, PortfolioRankBy rankBy)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100;

        var (items, total, rankingMap) = await _repo.GetAllAsync(page, pageSize, status, sort, rankBy);
        var mappedItems = items.Select(MapToDto).ToList();
        foreach (var item in mappedItems)
        {
            var hasRanking = rankingMap.TryGetValue(item.PortfolioId, out var ranking);
            item.Ranking = new RankingDto
            {
                TotalScore = hasRanking ? ranking.TotalScore : 0m,
                AverageScore = hasRanking ? ranking.AverageScore : 0m,
                RankPosition = hasRanking ? ranking.RankPosition : 0
            };
        }

        await PopulateReviewersAsync(mappedItems, mappedItems.Select(x => x.PortfolioId));

        return new PagedResult<PortfolioDto>
        {
            Items = mappedItems,
            Total = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<CreatePortfolioResponse> UpdateFullPortfolioAsync(
        int id,
        int employeeId,
        UpdateFullPortfolioRequest request,
        Dictionary<string, IFormFile> fileMap)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Name is required");

        var portfolio = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Portfolio {id} not found");

        if (portfolio.EmployeeId != employeeId)
            throw new UnauthorizedAccessException("You do not own this portfolio");

        var blockTypes = await _repo.GetBlockTypesAsync();
        var handlerMap = _handlers.ToDictionary(h => h.BlockType, StringComparer.OrdinalIgnoreCase);

        ValidateBlockMultiplicity(
            new CreatePortfolioRequest { Blocks = request.Blocks },
            blockTypes);

        using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

        portfolio.Name = request.Name;
        if (!string.IsNullOrWhiteSpace(request.Status))
            portfolio.Status = request.Status;
        if (request.IsPublic.HasValue)
            portfolio.IsPublic = request.IsPublic.Value;
        if (request.IsMain.HasValue)
            portfolio.IsMain = request.IsMain.Value;
        portfolio.UpdatedAt = VietnamTime.Now();

        if (request.IsMain == true)
            await _repo.SetMainPortfolioAsync(employeeId, portfolio.Id);

        // Remove all existing blocks
        portfolio.Blocks.Clear();

        foreach (var blockRequest in request.Blocks.OrderBy(b => b.Order))
        {
            if (!blockTypes.TryGetValue(blockRequest.Type.ToUpperInvariant(), out var blockType))
                throw new ArgumentException($"Unknown block type: {blockRequest.Type}");

            if (!handlerMap.TryGetValue(blockRequest.Type, out var handler))
                throw new InvalidOperationException($"No handler registered for block type: {blockRequest.Type}");

            var block = new PortfolioBlock
            {
                Portfolio = portfolio,
                BlockTypeId = blockType.Id,
                Variant = blockRequest.Variant,
                DisplayOrder = blockRequest.Order,
                IsVisible = true
            };

            await handler.HandleAsync(block, blockRequest.Data, fileMap);
            portfolio.Blocks.Add(block);
        }

        // Entity already tracked by EF Core, just commit changes
        await _repo.CommitAsync();

        scope.Complete();

        _logger.LogInformation("Portfolio {Id} fully updated with {BlockCount} blocks for employee {EmployeeId}",
            portfolio.Id, portfolio.Blocks.Count, employeeId);

        return new CreatePortfolioResponse { PortfolioId = portfolio.Id, Message = "Portfolio updated successfully" };
    }

    public async Task<CreatePortfolioResponse> CreatePortfolioAsync(
        CreatePortfolioRequest request,
        Dictionary<string, IFormFile> fileMap)
    {
        if (request.EmployeeId <= 0)
            throw new ArgumentException("EmployeeId is required");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Name is required");

        var blockTypes = await _repo.GetBlockTypesAsync();

        var handlerMap = _handlers.ToDictionary(h => h.BlockType, StringComparer.OrdinalIgnoreCase);

        ValidateBlockMultiplicity(request, blockTypes);

        using var scope = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);

        var portfolio = new Domain.Entities.Portfolio
        {
            EmployeeId = request.EmployeeId,
            Name = request.Name,
            Status = "active",
            IsMain = request.IsMain,
            IsPublic = request.IsPublic,
            CreatedAt = VietnamTime.Now()
        };

        foreach (var blockRequest in request.Blocks.OrderBy(b => b.Order))
        {
            if (!blockTypes.TryGetValue(blockRequest.Type.ToUpperInvariant(), out var blockType))
                throw new ArgumentException($"Unknown block type: {blockRequest.Type}");

            if (!handlerMap.TryGetValue(blockRequest.Type, out var handler))
                throw new InvalidOperationException($"No handler registered for block type: {blockRequest.Type}");

            var block = new PortfolioBlock
            {
                Portfolio = portfolio,
                BlockTypeId = blockType.Id,
                Variant = blockRequest.Variant,
                DisplayOrder = blockRequest.Order,
                IsVisible = true
            };

            await handler.HandleAsync(block, blockRequest.Data, fileMap);

            portfolio.Blocks.Add(block);
        }

        _repo.AddAsync(portfolio);
        await _repo.CommitAsync();
        if (request.IsMain)
        {
            await _repo.SetMainPortfolioAsync(request.EmployeeId, portfolio.Id);
            portfolio.IsMain = true;
        }

        scope.Complete();

        _logger.LogInformation("Portfolio {Id} created with {BlockCount} blocks for employee {EmployeeId}",
            portfolio.Id, portfolio.Blocks.Count, request.EmployeeId);

        return new CreatePortfolioResponse { PortfolioId = portfolio.Id, Message = "Portfolio created successfully" };
    }

    private static void ValidateBlockMultiplicity(
        CreatePortfolioRequest request,
        Dictionary<string, BlockType> blockTypes)
    {
        var typeCounts = request.Blocks
            .GroupBy(b => b.Type.ToUpperInvariant())
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (var (typeCode, count) in typeCounts)
        {
            if (blockTypes.TryGetValue(typeCode, out var bt) && !bt.IsMultiple && count > 1)
                throw new ArgumentException($"Block type '{typeCode}' does not allow multiple blocks (IsMultiple=false).");
        }
    }

    private static PortfolioDto MapToDto(Domain.Entities.Portfolio p) => new()
    {
        PortfolioId = p.Id,
        EmployeeId = p.EmployeeId,
        PortfolioName = p.Name,
        Status = p.Status,
        IsMain = p.IsMain,
        IsPublic = p.IsPublic,
        CreatedAt = p.CreatedAt,
        UpdatedAt = p.UpdatedAt,
        Blocks = new List<BlockDto>()
    };

    private async Task PopulateReviewersAsync(List<PortfolioDto> items, IEnumerable<int> portfolioIds)
    {
        if (items.Count == 0) return;

        var reviewerUserIdsByPortfolio = await _repo.GetReviewerUserIdsByPortfolioIdsAsync(portfolioIds);
        var allUserIds = reviewerUserIdsByPortfolio.Values.SelectMany(x => x).Distinct().ToList();
        if (allUserIds.Count == 0) return;

        var authUsers = await _authServiceClient.GetUsersByIdsAsync(allUserIds);
        var recruiterUserIds = authUsers.Values
            .Where(u => string.Equals(u.Role, "RECRUITER", StringComparison.OrdinalIgnoreCase))
            .Select(u => u.Id)
            .ToList();
        var expertUserIds = authUsers.Values
            .Where(u => string.Equals(u.Role, "EXPERT", StringComparison.OrdinalIgnoreCase))
            .Select(u => u.Id)
            .ToList();

        var recruiterProfiles = await _reviewerProfileClient.GetCompanyProfilesByUserIdsAsync(recruiterUserIds);
        var expertProfiles = await _reviewerProfileClient.GetExpertProfilesByUserIdsAsync(expertUserIds);

        foreach (var item in items)
        {
            if (!reviewerUserIdsByPortfolio.TryGetValue(item.PortfolioId, out var userIds))
            {
                item.Reviewers = new List<PortfolioReviewerDto>();
                continue;
            }

            item.Reviewers = BuildReviewers(userIds, authUsers, recruiterProfiles, expertProfiles);
        }
    }

    private async Task PopulateReviewersAsync(List<PortfolioWithComplimentDto> items, IEnumerable<int> portfolioIds)
    {
        if (items.Count == 0) return;

        var reviewerUserIdsByPortfolio = await _repo.GetReviewerUserIdsByPortfolioIdsAsync(portfolioIds);
        var allUserIds = reviewerUserIdsByPortfolio.Values.SelectMany(x => x).Distinct().ToList();
        if (allUserIds.Count == 0) return;

        var authUsers = await _authServiceClient.GetUsersByIdsAsync(allUserIds);
        var recruiterUserIds = authUsers.Values
            .Where(u => string.Equals(u.Role, "RECRUITER", StringComparison.OrdinalIgnoreCase))
            .Select(u => u.Id)
            .ToList();
        var expertUserIds = authUsers.Values
            .Where(u => string.Equals(u.Role, "EXPERT", StringComparison.OrdinalIgnoreCase))
            .Select(u => u.Id)
            .ToList();

        var recruiterProfiles = await _reviewerProfileClient.GetCompanyProfilesByUserIdsAsync(recruiterUserIds);
        var expertProfiles = await _reviewerProfileClient.GetExpertProfilesByUserIdsAsync(expertUserIds);

        foreach (var item in items)
        {
            if (!reviewerUserIdsByPortfolio.TryGetValue(item.Id, out var userIds))
            {
                item.Reviewers = new List<PortfolioReviewerDto>();
                continue;
            }

            item.Reviewers = BuildReviewers(userIds, authUsers, recruiterProfiles, expertProfiles);
        }
    }

    private static List<PortfolioReviewerDto> BuildReviewers(
        IEnumerable<int> userIds,
        Dictionary<int, AuthInternalUserDto> authUsers,
        Dictionary<int, ReviewerProfileDto> recruiterProfiles,
        Dictionary<int, ReviewerProfileDto> expertProfiles)
    {
        return userIds
            .Select(userId =>
            {
                if (!authUsers.TryGetValue(userId, out var authUser))
                {
                    return null;
                }

                var role = authUser.Role;
                if (!string.Equals(role, "RECRUITER", StringComparison.OrdinalIgnoreCase)
                    && !string.Equals(role, "EXPERT", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                ReviewerProfileDto? profile = null;
                if (string.Equals(role, "RECRUITER", StringComparison.OrdinalIgnoreCase))
                {
                    recruiterProfiles.TryGetValue(userId, out profile);
                }
                else
                {
                    expertProfiles.TryGetValue(userId, out profile);
                }

                return new PortfolioReviewerDto
                {
                    UserId = userId,
                    Role = role,
                    Name = profile?.Name,
                    Avatar = profile?.Avatar
                };
            })
            .Where(x => x != null)
            .Cast<PortfolioReviewerDto>()
            .ToList();
    }

    public async Task<PagedResult<PortfolioWithComplimentDto>> GetAllWithComplimentFilterAsync(PortfolioQueryParams queryParams)
    {
        if (queryParams.Page < 1) queryParams.Page = 1;
        if (queryParams.PageSize < 1) queryParams.PageSize = 10;
        if (queryParams.PageSize > 100) queryParams.PageSize = 100;

        var (items, total) = await _repo.GetAllWithComplimentFilterAsync(queryParams);
        await PopulateReviewersAsync(items, items.Select(x => x.Id));
        return new PagedResult<PortfolioWithComplimentDto>
        {
            Items = items,
            Total = total,
            Page = queryParams.Page,
            PageSize = queryParams.PageSize
        };
    }
}

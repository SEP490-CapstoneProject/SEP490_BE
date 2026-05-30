using Microsoft.EntityFrameworkCore;
using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;
using Portfolio.Domain.Entities;
using Portfolio.Infrastructure.Data;
using RecruitmentPlatform.AI.Models;
using RecruitmentPlatform.AI.Models;

namespace Portfolio.Infrastructure.Repositories;

public class PortfolioRepository : IPortfolioRepository
{
    private readonly PortfolioDbContext _context;
    private readonly ICurrentUserService _currentUser;

    public PortfolioRepository(PortfolioDbContext context, ICurrentUserService currentUser)
    {
        _context = context;
        _currentUser = currentUser;
    }

    public async Task<Portfolio.Domain.Entities.Portfolio?> GetByIdAsync(int id)
        => await _context.Portfolios
            .Include(p => p.Blocks)
            .ThenInclude(b => b.BlockType)
            .FirstOrDefaultAsync(p => p.Id == id);

    public async Task<IEnumerable<Portfolio.Domain.Entities.Portfolio>> GetByEmployeeIdAsync(int employeeId)
        => await _context.Portfolios
            .Where(p => p.EmployeeId == employeeId)
            .Include(p => p.Blocks)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

    public async Task<Portfolio.Domain.Entities.Portfolio?> GetMainByEmployeeIdAsync(int employeeId)
        => await _context.Portfolios
            .Where(p => p.EmployeeId == employeeId && p.IsMain)
            .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
            .FirstOrDefaultAsync();

    public async Task<int> CountByEmployeeIdAsync(int employeeId)
        => await _context.Portfolios.CountAsync(p => p.EmployeeId == employeeId);

    public async Task<(List<Portfolio.Domain.Entities.Portfolio> Items, int Total, Dictionary<int, (decimal TotalScore, decimal AverageScore, int RankPosition)> RankingMap)> GetAllAsync(int page, int pageSize, string? status, string? searchTerm, string? blockType, PortfolioSortMode sort, PortfolioRankBy rankBy)
    {
        var query = _context.Portfolios
            .Where(p => p.IsPublic && p.Status == "active")
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(p => p.Status == status);

        query = ApplySearchFilter(query, searchTerm, blockType);

        var total = await query.CountAsync();
        var items = await query.ToListAsync();

        var rankingMap = await GetPublicRankingMapAsync(rankBy);
        items = ApplySort(items, sort, rankingMap)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return (items, total, rankingMap);
    }

    public async Task<(List<Portfolio.Domain.Entities.Portfolio> Items, int Total)> GetPendingForModerationAsync(int page, int pageSize)
    {
        var query = _context.Portfolios
            .Where(p => p.ModerationStatus == "PendingReview")
            .OrderByDescending(p => p.ModeratedAt ?? p.UpdatedAt ?? p.CreatedAt)
            .ThenByDescending(p => p.Id);

        var total = await query.CountAsync();
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, total);
    }

    public async Task<bool> ExistsByEmployeeIdAsync(int employeeId)
        => await _context.Portfolios.AnyAsync(p => p.EmployeeId == employeeId);

    public async Task<Portfolio.Domain.Entities.Portfolio> CreateAsync(Portfolio.Domain.Entities.Portfolio portfolio)
    {
        _context.Portfolios.Add(portfolio);
        await _context.SaveChangesAsync();
        return portfolio;
    }

    public async Task<Portfolio.Domain.Entities.Portfolio> UpdateAsync(Portfolio.Domain.Entities.Portfolio portfolio)
    {
        _context.Portfolios.Update(portfolio);
        await _context.SaveChangesAsync();
        return portfolio;
    }

    public async Task SetMainPortfolioAsync(int employeeId, int portfolioId)
    {
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
UPDATE [Portfolio]
SET [IsMain] = CASE WHEN [Id] = {portfolioId} THEN CAST(1 AS bit) ELSE CAST(0 AS bit) END
WHERE [EmployeeId] = {employeeId}
  AND ([IsMain] = CAST(1 AS bit) OR [Id] = {portfolioId});");
    }

    public async Task<bool> DeleteAsync(int id)
    {
        var portfolio = await _context.Portfolios.FindAsync(id);
        if (portfolio == null) return false;
        _context.Portfolios.Remove(portfolio);
        await _context.SaveChangesAsync();
        return true;
    }

    public void AddAsync(Portfolio.Domain.Entities.Portfolio portfolio)
    {
        _context.Portfolios.Add(portfolio);
    }

    public async Task CommitAsync()
    {
        await _context.SaveChangesAsync();
    }

    public async Task<Dictionary<string, BlockType>> GetBlockTypesAsync()
        => await _context.BlockTypes.Where(x => x.IsActive).ToDictionaryAsync(x => x.Code);

    public async Task<Dictionary<int, List<int>>> GetReviewerUserIdsByPortfolioIdsAsync(IEnumerable<int> portfolioIds)
    {
        var idList = portfolioIds.Distinct().ToList();
        if (idList.Count == 0)
        {
            return new Dictionary<int, List<int>>();
        }

        var rows = await _context.Compliments
            .IgnoreQueryFilters()
            .Where(c => idList.Contains(c.PortfolioId) && c.State != ComplimentState.Deleted)
            .Select(c => new { c.PortfolioId, c.UserId })
            .Distinct()
            .ToListAsync();

        return rows
            .GroupBy(x => x.PortfolioId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.UserId).ToList());
    }

    public async Task<(List<PortfolioWithComplimentDto> Items, int Total)> GetAllWithComplimentFilterAsync(PortfolioQueryParams queryParams)
    {
        var query = _context.Portfolios
            .Where(p => p.IsPublic && p.Status == "active")
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(queryParams.Status))
            query = query.Where(p => p.Status == queryParams.Status);

        if (queryParams.HasCompliment.HasValue)
        {
            if (queryParams.HasCompliment.Value)
                query = query.Where(p => _context.Compliments.IgnoreQueryFilters()
                    .Any(c => c.PortfolioId == p.Id && c.State != ComplimentState.Deleted
                           && (_currentUser.IsAdmin || c.UserId == _currentUser.UserId)
                           && (!queryParams.ComplimentState.HasValue || c.State == queryParams.ComplimentState)));
            else
                query = query.Where(p => !_context.Compliments.IgnoreQueryFilters()
                    .Any(c => c.PortfolioId == p.Id && c.State != ComplimentState.Deleted
                           && (_currentUser.IsAdmin || c.UserId == _currentUser.UserId)));
        }
        else if (queryParams.ComplimentState.HasValue)
        {
            query = query.Where(p => _context.Compliments.IgnoreQueryFilters()
                .Any(c => c.PortfolioId == p.Id && c.State != ComplimentState.Deleted && c.State == queryParams.ComplimentState
                       && (_currentUser.IsAdmin || c.UserId == _currentUser.UserId)));
        }

        query = ApplySearchFilter(query, queryParams.SearchTerm, queryParams.BlockType);

        var total = await query.CountAsync();

        var items = await query
            .Select(p => new PortfolioWithComplimentDto
            {
                Id = p.Id,
                EmployeeId = p.EmployeeId,
                Name = p.Name,
                Status = p.Status,
                IsMain = p.IsMain,
                IsPublic = p.IsPublic,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                ComplimentCount = p.ComplimentCount,
                ApprovedComplimentCount = p.ApprovedComplimentCount,
                AverageScore = p.AverageScore,
                Compliments = !queryParams.IncludeCompliments ? null :
                    _context.Compliments.IgnoreQueryFilters()
                        .Where(c => c.PortfolioId == p.Id && c.State != ComplimentState.Deleted
                                 && (_currentUser.IsAdmin || c.UserId == _currentUser.UserId)
                                 && (!queryParams.ComplimentState.HasValue || c.State == queryParams.ComplimentState))
                        .Select(c => new ComplimentDto
                        {
                            Id = c.Id,
                            PortfolioId = c.PortfolioId,
                            UserId = c.UserId,
                            Content = c.Content,
                            Score = c.Score,
                            State = c.State,
                            CreatedAt = c.CreatedAt,
                            UpdatedAt = c.UpdatedAt
                        })
                        .ToList()
            })
            .ToListAsync();

        var rankingMap = await GetPublicRankingMapAsync(queryParams.RankBy);

        foreach (var item in items)
        {
            var hasRanking = rankingMap.TryGetValue(item.Id, out var ranking);
            item.Ranking = new RankingDto
            {
                TotalScore = hasRanking ? ranking.TotalScore : 0m,
                AverageScore = hasRanking ? ranking.AverageScore : 0m,
                RankPosition = hasRanking ? ranking.RankPosition : 0
            };
        }

        items = ApplySort(items, queryParams.Sort, rankingMap)
            .Skip((queryParams.Page - 1) * queryParams.PageSize)
            .Take(queryParams.PageSize)
            .ToList();

        return (items, total);
    }

    private IQueryable<Portfolio.Domain.Entities.Portfolio> ApplySearchFilter(
        IQueryable<Portfolio.Domain.Entities.Portfolio> query,
        string? searchTerm,
        string? blockType)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return query;
        }

        var normalizedSearch = searchTerm.Trim();
        var escapedSearch = EscapeLikePattern(normalizedSearch);
        var likePattern = $"%{escapedSearch}%";
        var normalizedBlockType = string.IsNullOrWhiteSpace(blockType) ? null : blockType.Trim().ToUpperInvariant();

        return query.Where(p => _context.PortfolioBlocks.Any(b =>
            b.PortfolioId == p.Id &&
            (normalizedBlockType == null || b.BlockType.Code == normalizedBlockType) &&
            EF.Functions.Like(b.DataJson, likePattern)));
    }

    private static string EscapeLikePattern(string value)
    {
        return value
            .Replace("[", "[[]")
            .Replace("%", "[%]")
            .Replace("_", "[_]");
    }

    private async Task<Dictionary<int, (decimal TotalScore, decimal AverageScore, int RankPosition)>> GetPublicRankingMapAsync(PortfolioRankBy rankBy)
    {
        var scoreMap = await _context.Compliments
            .IgnoreQueryFilters()
            .Where(c => c.State != ComplimentState.Deleted && c.Score != null)
            .GroupBy(c => c.PortfolioId)
            .Select(g => new
            {
                PortfolioId = g.Key,
                TotalScore = g.Sum(x => (decimal?)x.Score) ?? 0m,
                AverageScore = g.Average(x => (decimal?)x.Score) ?? 0m
            })
            .ToDictionaryAsync(x => x.PortfolioId, x => (x.TotalScore, x.AverageScore));

        var publicPortfolioIds = await _context.Portfolios
            .Where(p => p.IsPublic && p.Status == "active")
            .Select(p => p.Id)
            .ToListAsync();

        var publicPortfolioIdSet = publicPortfolioIds.ToHashSet();
        var rankSource = scoreMap
            .Where(x => publicPortfolioIdSet.Contains(x.Key))
            .Select(x => new
            {
                PortfolioId = x.Key,
                TotalScore = x.Value.TotalScore,
                AverageScore = x.Value.AverageScore
            });

        var orderedRankSource = rankBy == PortfolioRankBy.total
            ? rankSource.OrderByDescending(x => x.TotalScore)
                        .ThenByDescending(x => x.AverageScore)
                        .ThenBy(x => x.PortfolioId)
            : rankSource.OrderByDescending(x => x.AverageScore)
                        .ThenByDescending(x => x.TotalScore)
                        .ThenBy(x => x.PortfolioId);

        return orderedRankSource
            .Select((x, idx) => new
            {
                x.PortfolioId,
                x.TotalScore,
                x.AverageScore,
                RankPosition = idx + 1
            })
            .ToDictionary(
                x => x.PortfolioId,
                x => (x.TotalScore, x.AverageScore, x.RankPosition));
    }

    private static IEnumerable<Portfolio.Domain.Entities.Portfolio> ApplySort(
        IEnumerable<Portfolio.Domain.Entities.Portfolio> source,
        PortfolioSortMode sort,
        Dictionary<int, (decimal TotalScore, decimal AverageScore, int RankPosition)> rankingMap)
    {
        return sort switch
        {
            PortfolioSortMode.rank_asc => source
                .OrderBy(x =>
                {
                    var rank = rankingMap.TryGetValue(x.Id, out var r) ? r.RankPosition : 0;
                    return rank == 0 ? int.MaxValue : rank;
                })
                .ThenByDescending(x => x.UpdatedAt ?? x.CreatedAt),
            PortfolioSortMode.rank_desc => source
                .OrderByDescending(x => rankingMap.TryGetValue(x.Id, out var r) ? r.RankPosition : 0)
                .ThenByDescending(x => x.UpdatedAt ?? x.CreatedAt),
            PortfolioSortMode.random => source.OrderBy(_ => Guid.NewGuid()),
            PortfolioSortMode.oldest => source.OrderBy(x => x.UpdatedAt ?? x.CreatedAt),
            _ => source.OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
        };
    }

    private static IEnumerable<PortfolioWithComplimentDto> ApplySort(
        IEnumerable<PortfolioWithComplimentDto> source,
        PortfolioSortMode sort,
        Dictionary<int, (decimal TotalScore, decimal AverageScore, int RankPosition)> rankingMap)
    {
        return sort switch
        {
            PortfolioSortMode.rank_asc => source
                .OrderBy(x =>
                {
                    var rank = rankingMap.TryGetValue(x.Id, out var r) ? r.RankPosition : 0;
                    return rank == 0 ? int.MaxValue : rank;
                })
                .ThenByDescending(x => x.UpdatedAt ?? x.CreatedAt),
            PortfolioSortMode.rank_desc => source
                .OrderByDescending(x => rankingMap.TryGetValue(x.Id, out var r) ? r.RankPosition : 0)
                .ThenByDescending(x => x.UpdatedAt ?? x.CreatedAt),
            PortfolioSortMode.random => source.OrderBy(_ => Guid.NewGuid()),
            PortfolioSortMode.oldest => source.OrderBy(x => x.UpdatedAt ?? x.CreatedAt),
            _ => source.OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
        };
    }

    public async Task<List<Portfolio.Domain.Entities.Portfolio>> GetPublicPortfoliosForMatchingAsync(int limit)
    {
        var safeLimit = Math.Clamp(limit, 1, 150);
        return await _context.Portfolios
            .AsNoTracking()
            .Include(p => p.Blocks)
            .Where(p => p.IsPublic
                && p.Status == "active"
                && p.Embedding != null
                && p.EmbeddingStatus == "Ready")
            .OrderByDescending(p => p.EmbeddingUpdatedAt ?? p.UpdatedAt ?? p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Take(safeLimit)
            .ToListAsync();
    }

    public async Task<List<Portfolio.Domain.Entities.Portfolio>> GetPortfoliosForEmbeddingBackfillAsync(int limit)
    {
        var safeLimit = Math.Clamp(limit, 1, 200);
        return await _context.Portfolios
            .Include(p => p.Blocks)
            .Where(p => p.Embedding == null || p.EmbeddingStatus != EmbeddingReadinessPolicy.Ready)
            .OrderByDescending(p => p.UpdatedAt ?? p.CreatedAt)
            .ThenByDescending(p => p.Id)
            .Take(safeLimit)
            .ToListAsync();
    }

    public async Task<List<Portfolio.Domain.Entities.Portfolio>> GetPortfoliosByIdsAsync(IEnumerable<int> ids)
    {
        var idList = ids.Distinct().ToList();
        if (idList.Count == 0) return new List<Portfolio.Domain.Entities.Portfolio>();
        return await _context.Portfolios
            .AsNoTracking()
            .Include(p => p.Blocks)
                .ThenInclude(b => b.BlockType)
            .Where(p => idList.Contains(p.Id))
            .ToListAsync();
    }

    public async Task UpdateEmbeddingAsync(int portfolioId, string? embedding, int embeddingVersion, DateTime? embeddingUpdatedAt, string embeddingStatus)
    {
        var portfolio = await _context.Portfolios.FirstOrDefaultAsync(p => p.Id == portfolioId);
        if (portfolio == null) return;

        portfolio.Embedding = embedding;
        portfolio.EmbeddingVersion = embeddingVersion;
        portfolio.EmbeddingUpdatedAt = embeddingUpdatedAt;
        portfolio.EmbeddingStatus = embeddingStatus;
        await _context.SaveChangesAsync();
    }

    public async Task<PortfolioReport> CreatePortfolioReportAsync(PortfolioReport report)
    {
        _context.PortfolioReports.Add(report);
        await _context.SaveChangesAsync();
        return report;
    }

    public async Task<PortfolioReport?> GetPortfolioReportByIdAndReporterAsync(int portfolioId, int reporterUserId)
    {
        return await _context.PortfolioReports
            .FirstOrDefaultAsync(r => r.PortfolioId == portfolioId && r.ReporterUserId == reporterUserId);
    }

    public async Task<(List<PortfolioReport> Items, int Total)> GetPortfolioReportsAsync(int page, int pageSize)
    {
        var query = _context.PortfolioReports
            .AsNoTracking()
            .OrderByDescending(r => r.CreatedAt)
            .ThenByDescending(r => r.Id);

        var total = await query.CountAsync();
        var items = await query
            .Skip((Math.Max(1, page) - 1) * Math.Clamp(pageSize, 1, 100))
            .Take(Math.Clamp(pageSize, 1, 100))
            .ToListAsync();

        return (items, total);
    }

    public async Task<PortfolioReport?> GetPortfolioReportByIdAsync(int reportId)
    {
        return await _context.PortfolioReports
            .Include(r => r.Portfolio)
            .FirstOrDefaultAsync(r => r.Id == reportId);
    }

    public async Task UpdatePortfolioReportAsync(PortfolioReport report)
    {
        _context.PortfolioReports.Update(report);
        await _context.SaveChangesAsync();
    }
}

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;
using Portfolio.Domain.Entities;
using Portfolio.Infrastructure.Data;

namespace Portfolio.Infrastructure.Services;

public class ComplimentService : IComplimentService
{
    private readonly IComplimentRepository _repo;
    private readonly ICurrentUserService _currentUser;
    private readonly PortfolioDbContext _db;
    private readonly ILogger<ComplimentService> _logger;

    public ComplimentService(
        IComplimentRepository repo,
        ICurrentUserService currentUser,
        PortfolioDbContext db,
        ILogger<ComplimentService> logger)
    {
        _repo = repo;
        _currentUser = currentUser;
        _db = db;
        _logger = logger;
    }

    public async Task<ComplimentDto> CreateAsync(CreateComplimentRequest request)
    {
        if (!_currentUser.HasCompany)
            throw new UnauthorizedAccessException("Company identity required.");

        var compliment = new Compliment
        {
            PortfolioId = request.PortfolioId,
            CompanyId = _currentUser.CompanyId,
            Content = request.Content,
            Score = request.Score,
            State = ComplimentState.Pending,
            CreatedBy = _currentUser.CompanyId,
            CreatedAt = DateTime.UtcNow
        };

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var created = await _repo.CreateAsync(compliment);
            await RecalculateAggregatesAsync(request.PortfolioId);
            await tx.CommitAsync();
            _logger.LogInformation("Compliment {Id} created for portfolio {PortfolioId}", created.Id, request.PortfolioId);
            return MapToDto(created);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            await tx.RollbackAsync();
            throw new InvalidOperationException("This company has already complimented this portfolio.");
        }
    }

    public async Task<List<ComplimentDto>> GetByPortfolioAsync(int portfolioId)
    {
        if (!_currentUser.HasCompany && !_currentUser.IsAdmin)
            throw new UnauthorizedAccessException("Authentication required.");

        var list = await _repo.GetByPortfolioAndCompanyAsync(portfolioId, _currentUser.CompanyId);
        return list.Select(MapToDto).ToList();
    }

    public async Task<ComplimentDto> UpdateAsync(int id, UpdateComplimentRequest request)
    {
        var compliment = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Compliment {id} not found.");

        if (!_currentUser.IsAdmin && compliment.CompanyId != _currentUser.CompanyId)
            throw new UnauthorizedAccessException("You do not own this compliment.");

        compliment.Content = request.Content;
        compliment.Score = request.Score;
        compliment.UpdatedAt = DateTime.UtcNow;
        compliment.UpdatedBy = _currentUser.CompanyId;

        await using var tx = await _db.Database.BeginTransactionAsync();
        var updated = await _repo.UpdateAsync(compliment);
        await RecalculateAggregatesAsync(compliment.PortfolioId);
        await tx.CommitAsync();

        return MapToDto(updated);
    }

    public async Task<ComplimentDto> PatchStateAsync(int id, PatchComplimentStateRequest request)
    {
        var compliment = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Compliment {id} not found.");

        if (!_currentUser.IsAdmin && compliment.CompanyId != _currentUser.CompanyId)
            throw new UnauthorizedAccessException("You do not own this compliment.");

        compliment.State = request.State;
        compliment.UpdatedAt = DateTime.UtcNow;
        compliment.UpdatedBy = _currentUser.CompanyId;

        await using var tx = await _db.Database.BeginTransactionAsync();
        var updated = await _repo.UpdateAsync(compliment);
        await RecalculateAggregatesAsync(compliment.PortfolioId);
        await tx.CommitAsync();

        return MapToDto(updated);
    }

    public async Task DeleteAsync(int id)
    {
        var compliment = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Compliment {id} not found.");

        if (!_currentUser.IsAdmin && compliment.CompanyId != _currentUser.CompanyId)
            throw new UnauthorizedAccessException("You do not own this compliment.");

        await using var tx = await _db.Database.BeginTransactionAsync();
        await _repo.SoftDeleteAsync(id);
        await RecalculateAggregatesAsync(compliment.PortfolioId);
        await tx.CommitAsync();
    }

    private async Task RecalculateAggregatesAsync(int portfolioId)
    {
        var count = await _db.Compliments
            .IgnoreQueryFilters()
            .CountAsync(c => c.PortfolioId == portfolioId && !c.IsDeleted);

        var approvedCount = await _db.Compliments
            .IgnoreQueryFilters()
            .CountAsync(c => c.PortfolioId == portfolioId && !c.IsDeleted && c.State == ComplimentState.Approved);

        var avgScore = await _db.Compliments
            .IgnoreQueryFilters()
            .Where(c => c.PortfolioId == portfolioId && !c.IsDeleted && c.Score != null)
            .Select(c => (decimal?)c.Score)
            .AverageAsync();

        await _db.Portfolios
            .Where(p => p.Id == portfolioId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.ComplimentCount, count)
                .SetProperty(p => p.ApprovedComplimentCount, approvedCount)
                .SetProperty(p => p.AverageScore, avgScore));
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
        => ex.InnerException?.Message.Contains("UNIQUE") == true
        || ex.InnerException?.Message.Contains("unique") == true
        || ex.InnerException?.Message.Contains("duplicate") == true;

    private static ComplimentDto MapToDto(Compliment c) => new()
    {
        Id = c.Id,
        PortfolioId = c.PortfolioId,
        CompanyId = c.CompanyId,
        Content = c.Content,
        Score = c.Score,
        State = c.State,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt
    };
}

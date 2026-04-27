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
    private readonly IEmployeeServiceClient _employeeServiceClient;
    private readonly IPortfolioNotificationEventPublisher _notificationEventPublisher;
    private readonly PortfolioDbContext _db;
    private readonly ILogger<ComplimentService> _logger;

    public ComplimentService(
        IComplimentRepository repo,
        ICurrentUserService currentUser,
        IEmployeeServiceClient employeeServiceClient,
        IPortfolioNotificationEventPublisher notificationEventPublisher,
        PortfolioDbContext db,
        ILogger<ComplimentService> logger)
    {
        _repo = repo;
        _currentUser = currentUser;
        _employeeServiceClient = employeeServiceClient;
        _notificationEventPublisher = notificationEventPublisher;
        _db = db;
        _logger = logger;
    }

    public async Task<ComplimentDto> CreateAsync(CreateComplimentRequest request)
    {
        if (!_currentUser.CanScorePortfolio || _currentUser.UserId <= 0)
            throw new UnauthorizedAccessException("Scoring role identity required.");
        if (!request.Score.HasValue)
            throw new ArgumentException("Score is required for compliment scoring.");

        var compliment = new Compliment
        {
            PortfolioId = request.PortfolioId,
            UserId = _currentUser.UserId,
            Content = request.Content,
            Score = request.Score,
            State = ComplimentState.Pending,
            CreatedBy = _currentUser.UserId,
            CreatedAt = DateTime.UtcNow
        };

        await using var tx = await _db.Database.BeginTransactionAsync();
        try
        {
            var created = await _repo.CreateAsync(compliment);
            await RecalculateAggregatesAsync(request.PortfolioId);
            await tx.CommitAsync();
            await TryPublishComplimentNotificationAsync(created);
            _logger.LogInformation("Compliment {Id} created for portfolio {PortfolioId}", created.Id, request.PortfolioId);
            return MapToDto(created);
        }
        catch (DbUpdateException ex) when (IsUniqueConstraintViolation(ex))
        {
            await tx.RollbackAsync();
            throw new InvalidOperationException("This user has already complimented this portfolio.");
        }
    }

    public async Task<List<ComplimentDto>> GetByPortfolioAsync(int portfolioId)
    {
        if (!_currentUser.CanScorePortfolio || _currentUser.UserId <= 0)
            throw new UnauthorizedAccessException("Authentication required.");

        var list = await _repo.GetByPortfolioAndUserAsync(portfolioId, _currentUser.UserId, _currentUser.IsAdmin);
        return list.Select(MapToDto).ToList();
    }

    public async Task<ComplimentDto> UpdateAsync(int id, UpdateComplimentRequest request)
    {
        var compliment = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Compliment {id} not found.");
        if (!request.Score.HasValue)
            throw new ArgumentException("Score is required for compliment scoring.");

        if (!_currentUser.IsAdmin && compliment.UserId != _currentUser.UserId)
            throw new UnauthorizedAccessException("You do not own this compliment.");

        compliment.Content = request.Content;
        compliment.Score = request.Score;
        compliment.UpdatedAt = DateTime.UtcNow;
        compliment.UpdatedBy = _currentUser.UserId;

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

        if (!_currentUser.IsAdmin && compliment.UserId != _currentUser.UserId)
            throw new UnauthorizedAccessException("You do not own this compliment.");

        compliment.State = request.State;
        compliment.UpdatedAt = DateTime.UtcNow;
        compliment.UpdatedBy = _currentUser.UserId;

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

        if (!_currentUser.IsAdmin && compliment.UserId != _currentUser.UserId)
            throw new UnauthorizedAccessException("You do not own this compliment.");

        await using var tx = await _db.Database.BeginTransactionAsync();
        await _repo.MarkDeletedAsync(id, _currentUser.UserId);
        await RecalculateAggregatesAsync(compliment.PortfolioId);
        await tx.CommitAsync();
    }

    private async Task RecalculateAggregatesAsync(int portfolioId)
    {
        var count = await _db.Compliments
            .IgnoreQueryFilters()
            .CountAsync(c => c.PortfolioId == portfolioId && c.State != ComplimentState.Deleted);

        var approvedCount = await _db.Compliments
            .IgnoreQueryFilters()
            .CountAsync(c => c.PortfolioId == portfolioId && c.State != ComplimentState.Deleted && c.State == ComplimentState.Approved);

        var avgScore = await _db.Compliments
            .IgnoreQueryFilters()
            .Where(c => c.PortfolioId == portfolioId && c.State != ComplimentState.Deleted && c.Score != null)
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

    private async Task TryPublishComplimentNotificationAsync(Compliment compliment)
    {
        var portfolio = await _db.Portfolios
            .AsNoTracking()
            .Where(p => p.Id == compliment.PortfolioId)
            .Select(p => new { p.Id, p.EmployeeId })
            .FirstOrDefaultAsync();

        if (portfolio is null)
        {
            return;
        }

        var employee = await _employeeServiceClient.GetEmployeeByIdAsync(portfolio.EmployeeId);
        if (employee is null || employee.UserId <= 0 || employee.UserId == _currentUser.UserId)
        {
            return;
        }

        var payload = new PortfolioNotificationEventPayload
        {
            EventType = "portfolio.compliment.created",
            UserId = employee.UserId.ToString(),
            ActorId = _currentUser.UserId.ToString(),
            ActorType = "RECRUITER",
            ObjectId = compliment.PortfolioId.ToString(),
            Title = "Portfolio đã được đánh giá",
            Content = "Portfolio của bạn vừa nhận được một đánh giá mới từ nhà tuyển dụng.",
            Type = "PORTFOLIO_REVIEWED",
            CreatedAt = compliment.CreatedAt
        };

        try
        {
            await _notificationEventPublisher.PublishComplimentCreatedAsync(payload);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to publish compliment notification. ComplimentId={ComplimentId}", compliment.Id);
        }
    }

    private static ComplimentDto MapToDto(Compliment c) => new()
    {
        Id = c.Id,
        PortfolioId = c.PortfolioId,
        UserId = c.UserId,
        Content = c.Content,
        Score = c.Score,
        State = c.State,
        CreatedAt = c.CreatedAt,
        UpdatedAt = c.UpdatedAt
    };
}

using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;
using Portfolio.Domain.Entities;

namespace Portfolio.Application.Services;

/// <summary>
/// Manages the complete lifecycle of reward points: earning from compliments, spending on redemptions, and balance tracking.
/// This is the central service for recruiter point management.
/// </summary>
public class RewardPointsService : IRewardPointsService
{
    private readonly IRewardPointTransactionRepository _transactionRepository;
    private readonly IComplimentPointEvaluator _evaluator;
    private readonly IComplimentRepository _complimentRepository;

    private const decimal PointsPerQualifyingCompliment = 1;
    private const int MaxPointsPerDay = 10;

    public RewardPointsService(
        IRewardPointTransactionRepository transactionRepository,
        IComplimentPointEvaluator evaluator,
        IComplimentRepository complimentRepository)
    {
        _transactionRepository = transactionRepository;
        _evaluator = evaluator;
        _complimentRepository = complimentRepository;
    }

    public async Task<bool> EarnPointsAsync(int complimentId, int recruiterId, string complimentContent, int portfolioId)
    {
        // Validate compliment qualifies for points
        var qualifies = await _evaluator.IsQualifyingComplimentAsync(
            portfolioId,
            recruiterId,
            complimentContent,
            excludeComplimentId: complimentId);
        if (!qualifies)
            return false;

        // Check daily cap
        var todayPoints = await GetTodayPointCountAsync(recruiterId);
        if (todayPoints >= MaxPointsPerDay)
            return false;

        // Create transaction record
        var transaction = new RewardPointTransaction
        {
            UserId = recruiterId,
            Points = PointsPerQualifyingCompliment,
            Type = RewardPointType.Earn,
            SourceType = RewardPointSourceType.ComplimentReview,
            SourceId = complimentId.ToString(),
            CreatedAt = DateTime.UtcNow
        };

        await _transactionRepository.AddAsync(transaction);
        return true;
    }

    public async Task<bool> SpendPointsAsync(int userId, decimal points, string sourceType, string sourceId)
    {
        // Validate sufficient balance
        var canSpend = await CanSpendPointsAsync(userId, points);
        if (!canSpend)
            return false;

        // Determine source type enum
        var sourceTypeEnum = sourceType switch
        {
            "SponsoredFeed" => RewardPointSourceType.SponsoredFeedRedemption,
            "Admin" => RewardPointSourceType.AdminAdjustment,
            _ => RewardPointSourceType.SponsoredFeedRedemption
        };

        // Create spend transaction
        var transaction = new RewardPointTransaction
        {
            UserId = userId,
            Points = points,
            Type = RewardPointType.Spend,
            SourceType = sourceTypeEnum,
            SourceId = sourceId,
            CreatedAt = DateTime.UtcNow
        };

        await _transactionRepository.AddAsync(transaction);
        return true;
    }

    public async Task<decimal> GetUserPointsAsync(int userId)
    {
        return await _transactionRepository.GetUserTotalPointsAsync(userId);
    }

    public async Task<decimal> GetTodayPointCountAsync(int userId)
    {
        return await _transactionRepository.GetUserTodayEarnedAsync(userId);
    }

    public async Task<bool> CanEarnPointsAsync(int portfolioId, int recruiterId, string complimentContent)
    {
        // Check all qualifications
        var qualifies = await _evaluator.IsQualifyingComplimentAsync(portfolioId, recruiterId, complimentContent);
        if (!qualifies)
            return false;

        // Check daily cap
        var todayPoints = await GetTodayPointCountAsync(recruiterId);
        return todayPoints < MaxPointsPerDay;
    }

    public async Task<bool> CanSpendPointsAsync(int userId, decimal points)
    {
        if (points <= 0)
            return false;

        var balance = await GetUserPointsAsync(userId);
        return balance >= points;
    }

    public async Task<List<RewardPointTransactionDto>> GetPointTransactionsAsync(int userId, int? lastDays = null)
    {
        List<RewardPointTransaction> transactions;

        if (lastDays.HasValue)
        {
            var endDate = DateTime.UtcNow;
            var startDate = endDate.AddDays(-lastDays.Value);
            transactions = await _transactionRepository.GetByUserIdAndDateRangeAsync(userId, startDate, endDate);
        }
        else
        {
            transactions = await _transactionRepository.GetByUserIdAsync(userId);
        }

        return transactions.Select(t => new RewardPointTransactionDto
        {
            Id = t.Id,
            UserId = t.UserId,
            Points = t.Points,
            Type = t.Type.ToString(),
            SourceType = t.SourceType.ToString(),
            SourceId = t.SourceId,
            Description = t.Description,
            CreatedAt = t.CreatedAt
        }).ToList();
    }

    public async Task<PointBalanceDto> GetPointBalanceAsync(int userId)
    {
        var transactions = await _transactionRepository.GetByUserIdAsync(userId);

        var totalEarned = transactions
            .Where(t => t.Type == RewardPointType.Earn)
            .Sum(t => t.Points);

        var totalSpent = transactions
            .Where(t => t.Type == RewardPointType.Spend)
            .Sum(t => t.Points);

        var currentBalance = totalEarned - totalSpent;
        
        var todayEarned = transactions
            .Where(t => t.Type == RewardPointType.Earn && t.CreatedAt.Date == DateTime.UtcNow.Date)
            .Count();

        return new PointBalanceDto
        {
            UserId = userId,
            CurrentBalance = currentBalance,
            TodayEarned = todayEarned,
            TotalEarned = totalEarned,
            TotalSpent = totalSpent,
            LastTransactionAt = transactions.OrderByDescending(t => t.CreatedAt).FirstOrDefault()?.CreatedAt
        };
    }
}

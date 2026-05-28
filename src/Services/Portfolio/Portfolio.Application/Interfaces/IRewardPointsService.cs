namespace Portfolio.Application.Interfaces;

using Portfolio.Application.DTOs;

/// <summary>
/// Manages the complete lifecycle of reward points: earning from compliments, spending on redemptions, and balance tracking.
/// Central service for recruiter point management and validation.
/// </summary>
public interface IRewardPointsService
{
    /// <summary>
    /// Awards points to recruiter after a quality compliment is created.
    /// Validates all business rules (quality, duplicate, spam, daily cap, recency).
    /// Returns true if points were awarded, false if compliment doesn't qualify.
    /// </summary>
    Task<bool> EarnPointsAsync(int complimentId, int recruiterId, string complimentContent, int portfolioId);

    /// <summary>
    /// Deducts points from recruiter's balance (e.g., for sponsored feed redemption).
    /// Validates recruiter has sufficient points.
    /// Returns true if points were spent, false if insufficient balance.
    /// </summary>
    Task<bool> SpendPointsAsync(int userId, decimal points, string sourceType, string sourceId);

    /// <summary>
    /// Gets recruiter's current total point balance (earned - spent).
    /// </summary>
    Task<decimal> GetUserPointsAsync(int userId);

    /// <summary>
    /// Gets total points earned today by recruiter.
    /// Used to enforce 10-point/day cap.
    /// </summary>
    Task<decimal> GetTodayPointCountAsync(int userId);

    /// <summary>
    /// Validates if recruiter can earn points for this compliment without violating business rules.
    /// </summary>
    Task<bool> CanEarnPointsAsync(int portfolioId, int recruiterId, string complimentContent);

    /// <summary>
    /// Validates if recruiter can spend the specified points (sufficient balance).
    /// </summary>
    Task<bool> CanSpendPointsAsync(int userId, decimal points);

    /// <summary>
    /// Gets transaction history for recruiter over last N days (default: all history).
    /// </summary>
    Task<List<RewardPointTransactionDto>> GetPointTransactionsAsync(int userId, int? lastDays = null);

    /// <summary>
    /// Gets detailed balance information including total points and breakdown of earn/spend.
    /// </summary>
    Task<PointBalanceDto> GetPointBalanceAsync(int userId);
}

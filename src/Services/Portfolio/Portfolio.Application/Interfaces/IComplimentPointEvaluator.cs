namespace Portfolio.Application.Interfaces;

/// <summary>
/// Evaluates compliments to determine if they qualify for reward points.
/// Implements validation rules for content quality, duplicates, spam, and recency.
/// </summary>
public interface IComplimentPointEvaluator
{
    /// <summary>
    /// Checks if compliment content meets quality threshold (≥20 characters).
    /// </summary>
    Task<bool> IsContentQualityAsync(string content);

    /// <summary>
    /// Checks if recruiter has already created compliment on this portfolio.
    /// </summary>
    Task<bool> IsDuplicateAsync(int portfolioId, int recruiterId, int? excludeComplimentId = null);

    /// <summary>
    /// Checks if content passes spam detection (no disallowed keywords, no excessive links, etc.).
    /// </summary>
    Task<bool> IsSpamAsync(string content);

    /// <summary>
    /// Checks if recruiter reviewed this portfolio within the specified days (default 30).
    /// </summary>
    Task<bool> HasRecentReviewAsync(int portfolioId, int recruiterId, int days = 30, int? excludeComplimentId = null);

    /// <summary>
    /// Comprehensive validation: content quality AND not duplicate AND not spam AND not recent review.
    /// </summary>
    Task<bool> IsQualifyingComplimentAsync(int portfolioId, int recruiterId, string content, int? excludeComplimentId = null);
}

using Portfolio.Application.Interfaces;

namespace Portfolio.Application.Services;

/// <summary>
/// Evaluates compliments to determine if they qualify for reward points.
/// Implements validation rules: content quality, duplicates, spam, and recency.
/// </summary>
public class ComplimentPointEvaluator : IComplimentPointEvaluator
{
    private readonly IComplimentRepository _complimentRepository;
    private const int MinContentLength = 20;
    private const int ReviewRecencyDays = 30;
    
    // Simple spam detection: disallowed keywords and excessive URLs
    private static readonly string[] SpamKeywords = new[] { "viagra", "casino", "lottery", "click here" };

    public ComplimentPointEvaluator(IComplimentRepository complimentRepository)
    {
        _complimentRepository = complimentRepository;
    }

    public Task<bool> IsContentQualityAsync(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Task.FromResult(false);

        return Task.FromResult(content.Length >= MinContentLength);
    }

    public async Task<bool> IsDuplicateAsync(int portfolioId, int recruiterId, int? excludeComplimentId = null)
    {
        return await _complimentRepository.HasComplimentFromCreatorAsync(portfolioId, recruiterId, excludeComplimentId);
    }

    public Task<bool> IsSpamAsync(string content)
    {
        if (string.IsNullOrWhiteSpace(content))
            return Task.FromResult(true);

        var lowerContent = content.ToLowerInvariant();

        // Check for disallowed keywords
        foreach (var keyword in SpamKeywords)
        {
            if (lowerContent.Contains(keyword))
                return Task.FromResult(true);
        }

        // Check for excessive URLs (more than 1)
        var urlCount = (lowerContent.Split("http://").Length - 1) + 
                       (lowerContent.Split("https://").Length - 1);
        if (urlCount > 1)
            return Task.FromResult(true);

        return Task.FromResult(false);
    }

    public async Task<bool> HasRecentReviewAsync(int portfolioId, int recruiterId, int days = 30, int? excludeComplimentId = null)
    {
        return await _complimentRepository.HasComplimentFromCreatorInLastDaysAsync(portfolioId, recruiterId, days, excludeComplimentId);
    }

    public async Task<bool> IsQualifyingComplimentAsync(int portfolioId, int recruiterId, string content, int? excludeComplimentId = null)
    {
        // All conditions must be true
        var isQuality = await IsContentQualityAsync(content);
        if (!isQuality)
            return false;

        var isDuplicate = await IsDuplicateAsync(portfolioId, recruiterId, excludeComplimentId);
        if (isDuplicate)
            return false;

        var isSpam = await IsSpamAsync(content);
        if (isSpam)
            return false;

        var hasRecentReview = await HasRecentReviewAsync(portfolioId, recruiterId, ReviewRecencyDays, excludeComplimentId);
        if (hasRecentReview)
            return false;

        return true;
    }
}

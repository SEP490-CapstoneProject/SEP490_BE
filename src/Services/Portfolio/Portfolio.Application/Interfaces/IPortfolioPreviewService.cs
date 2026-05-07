using Portfolio.Application.DTOs;

namespace Portfolio.Application.Interfaces;

public interface IPortfolioPreviewService
{
    /// <summary>
    /// Generate a new portfolio preview or update existing one
    /// </summary>
    Task<GeneratePreviewResponse?> GeneratePreviewAsync(int portfolioId, string? highlightsDescription = null);

    /// <summary>
    /// Get current preview for a portfolio
    /// </summary>
    Task<PortfolioPreviewResponse?> GetPreviewAsync(int portfolioId);

    /// <summary>
    /// Regenerate preview (increments version)
    /// </summary>
    Task<GeneratePreviewResponse?> RegeneratePreviewAsync(int portfolioId, string? newHighlights = null);
}

/// <summary>
/// Response when generating/regenerating preview
/// </summary>
public class GeneratePreviewResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public PortfolioPreviewResponse? Preview { get; set; }
}

/// <summary>
/// DTO for portfolio preview data
/// </summary>
public class PortfolioPreviewResponse
{
    public int Id { get; set; }
    public int PortfolioId { get; set; }
    public Dictionary<string, object>? PreviewJson { get; set; }
    public string? HighlightsDescription { get; set; }
    public int Version { get; set; }
    public int RegeneratedCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string GenerationModel { get; set; } = "gemini-1.5-pro";
    public int? TokensUsed { get; set; }
}

using Portfolio.Domain.Entities;

namespace Portfolio.Application.Interfaces;

public interface IPortfolioPreviewRepository
{
    /// <summary>
    /// Get preview by portfolio ID
    /// </summary>
    Task<PortfolioPreview?> GetByPortfolioIdAsync(int portfolioId);

    /// <summary>
    /// Create a new preview
    /// </summary>
    Task<PortfolioPreview> CreateAsync(PortfolioPreview preview);

    /// <summary>
    /// Update existing preview
    /// </summary>
    Task<PortfolioPreview> UpdateAsync(PortfolioPreview preview);

    /// <summary>
    /// Check if preview exists for portfolio
    /// </summary>
    Task<bool> ExistsByPortfolioIdAsync(int portfolioId);

    /// <summary>
    /// Delete preview by ID
    /// </summary>
    Task<bool> DeleteAsync(int id);
}

using Community.Application.DTOs;

namespace Community.Application.Clients;

public interface IPortfolioPreviewClient
{
    /// <summary>Returns the first block of a portfolio as preview, or null if not found.</summary>
    Task<PortfolioPreviewDto?> GetPreviewAsync(int portfolioId);
}

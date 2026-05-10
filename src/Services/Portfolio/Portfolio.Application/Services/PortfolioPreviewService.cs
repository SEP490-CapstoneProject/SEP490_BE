using System.Text.Json;
using Microsoft.Extensions.Logging;
using Portfolio.Application.Interfaces;
using Portfolio.Domain.Entities;

namespace Portfolio.Application.Services;

public class PortfolioPreviewService : IPortfolioPreviewService
{
    private readonly IPortfolioRepository _portfolioRepo;
    private readonly IPortfolioPreviewRepository _previewRepo;
    private readonly GoogleAiPreviewGenerator _aiGenerator;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<PortfolioPreviewService> _logger;

    public PortfolioPreviewService(
        IPortfolioRepository portfolioRepo,
        IPortfolioPreviewRepository previewRepo,
        GoogleAiPreviewGenerator aiGenerator,
        ICurrentUserService currentUser,
        ILogger<PortfolioPreviewService> logger)
    {
        _portfolioRepo = portfolioRepo;
        _previewRepo = previewRepo;
        _aiGenerator = aiGenerator;
        _currentUser = currentUser;
        _logger = logger;
    }

    public async Task<GeneratePreviewResponse?> GeneratePreviewAsync(int portfolioId, string? highlightsDescription = null)
    {
        try
        {
            _logger.LogInformation("Generating preview for portfolio {PortfolioId}", portfolioId);

            // Get portfolio with blocks
            var portfolio = await _portfolioRepo.GetByIdAsync(portfolioId);
            if (portfolio == null)
            {
                _logger.LogWarning("❌ Portfolio {PortfolioId} not found", portfolioId);
                return new GeneratePreviewResponse 
                { 
                    Success = false, 
                    Message = $"Portfolio {portfolioId} not found" 
                };
            }

            // Check authorization (owner only)
            if (portfolio.EmployeeId != _currentUser.UserId && !_currentUser.IsAdmin)
            {
                _logger.LogWarning("❌ User {UserId} not authorized to generate preview for portfolio {PortfolioId}", 
                    _currentUser.UserId, portfolioId);
                return new GeneratePreviewResponse 
                { 
                    Success = false, 
                    Message = "Not authorized to generate preview for this portfolio" 
                };
            }

            // Check portfolio is approved
            if (portfolio.ModerationStatus != "Approved")
            {
                _logger.LogWarning("⚠️ Portfolio {PortfolioId} not approved. Status: {Status}", portfolioId, portfolio.ModerationStatus);
                return new GeneratePreviewResponse 
                { 
                    Success = false, 
                    Message = $"Portfolio must be approved before generating preview. Current status: {portfolio.ModerationStatus}" 
                };
            }

            // Generate preview via AI
            var (success, previewJson, errorMessage, tokensUsed) = await _aiGenerator.GeneratePreviewAsync(
                portfolio,
                portfolio.Blocks,
                highlightsDescription
            );

            if (!success)
            {
                _logger.LogError("❌ AI preview generation failed: {Error}", errorMessage);
                return new GeneratePreviewResponse 
                { 
                    Success = false, 
                    Message = $"Preview generation failed: {errorMessage}" 
                };
            }

            // Check if preview already exists
            var existingPreview = await _previewRepo.GetByPortfolioIdAsync(portfolioId);

            PortfolioPreview preview;
            if (existingPreview != null)
            {
                // Update existing preview
                existingPreview.PreviewJson = previewJson!;
                existingPreview.HighlightsDescription = highlightsDescription;
                existingPreview.Version++;
                existingPreview.RegeneratedCount++;
                existingPreview.UpdatedAt = DateTime.UtcNow;
                existingPreview.TokensUsed = tokensUsed;

                preview = await _previewRepo.UpdateAsync(existingPreview);
                _logger.LogInformation("✅ Portfolio preview updated. Version: {Version}, Regenerated: {Count}", 
                    preview.Version, preview.RegeneratedCount);
            }
            else
            {
                // Create new preview
                preview = new PortfolioPreview
                {
                    PortfolioId = portfolioId,
                    PreviewJson = previewJson!,
                    HighlightsDescription = highlightsDescription,
                    Version = 1,
                    RegeneratedCount = 0,
                    TokensUsed = tokensUsed,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                preview = await _previewRepo.CreateAsync(preview);
                _logger.LogInformation("✅ Portfolio preview created successfully");
            }

            // Return response
            var response = MapToResponse(preview);
            return new GeneratePreviewResponse 
            { 
                Success = true, 
                Message = "Preview generated successfully",
                Preview = response
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Unexpected error generating preview for portfolio {PortfolioId}", portfolioId);
            return new GeneratePreviewResponse 
            { 
                Success = false, 
                Message = $"Unexpected error: {ex.Message}" 
            };
        }
    }

    public async Task<PortfolioPreviewResponse?> GetPreviewAsync(int portfolioId)
    {
        try
        {
            _logger.LogInformation("Retrieving preview for portfolio {PortfolioId}", portfolioId);

            // Get portfolio to check permissions
            var portfolio = await _portfolioRepo.GetByIdAsync(portfolioId);
            if (portfolio == null)
            {
                _logger.LogWarning("Portfolio {PortfolioId} not found", portfolioId);
                return null;
            }

            // Check authorization: public if portfolio.IsPublic, otherwise owner/admin only
            if (!portfolio.IsPublic && portfolio.EmployeeId != _currentUser.UserId && !_currentUser.IsAdmin)
            {
                _logger.LogWarning("User {UserId} not authorized to view preview for portfolio {PortfolioId}", 
                    _currentUser.UserId, portfolioId);
                return null;
            }

            // Get preview
            var preview = await _previewRepo.GetByPortfolioIdAsync(portfolioId);
            if (preview == null)
            {
                _logger.LogInformation("No preview found for portfolio {PortfolioId}", portfolioId);
                return null;
            }

            _logger.LogInformation("✅ Preview retrieved successfully");
            return MapToResponse(preview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error retrieving preview for portfolio {PortfolioId}", portfolioId);
            return null;
        }
    }

    public async Task<GeneratePreviewResponse?> RegeneratePreviewAsync(int portfolioId, string? newHighlights = null)
    {
        return await GeneratePreviewAsync(portfolioId, newHighlights);
    }

    /// <summary>
    /// Map PortfolioPreview entity to PortfolioPreviewResponse DTO
    /// </summary>
    private PortfolioPreviewResponse MapToResponse(PortfolioPreview preview)
    {
        var previewJson = ParsePreviewJson(preview.PreviewJson);

        return new PortfolioPreviewResponse
        {
            Id = preview.Id,
            PortfolioId = preview.PortfolioId,
            PreviewJson = previewJson,
            HighlightsDescription = preview.HighlightsDescription,
            Version = preview.Version,
            RegeneratedCount = preview.RegeneratedCount,
            CreatedAt = preview.CreatedAt,
            UpdatedAt = preview.UpdatedAt,
            GenerationModel = preview.GenerationModel,
            TokensUsed = preview.TokensUsed
        };
    }

    /// <summary>
    /// Parse JSON string to dictionary
    /// </summary>
    private Dictionary<string, object>? ParsePreviewJson(string jsonString)
    {
        try
        {
            using var jsonDoc = JsonDocument.Parse(jsonString);
            var dict = new Dictionary<string, object>();

            foreach (var prop in jsonDoc.RootElement.EnumerateObject())
            {
                dict[prop.Name] = prop.Value.GetString() ?? prop.Value.ToString();
            }

            return dict;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error parsing preview JSON");
            return null;
        }
    }
}

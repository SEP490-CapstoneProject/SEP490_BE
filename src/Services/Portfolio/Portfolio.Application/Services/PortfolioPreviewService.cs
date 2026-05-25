using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;
using Portfolio.Domain.Entities;

namespace Portfolio.Application.Services;

public class PortfolioPreviewService : IPortfolioPreviewService
{
    private readonly IPortfolioRepository _portfolioRepo;
    private readonly IPortfolioPreviewRepository _previewRepo;
    private readonly GoogleAiPreviewGenerator _aiGenerator;
    private readonly VisualPromptService _visualPromptService;
    private readonly ImageGenerationService _imageGenerationService;
    private readonly ICurrentUserService _currentUser;
    private readonly ILogger<PortfolioPreviewService> _logger;

    public PortfolioPreviewService(
        IPortfolioRepository portfolioRepo,
        IPortfolioPreviewRepository previewRepo,
        GoogleAiPreviewGenerator aiGenerator,
        VisualPromptService visualPromptService,
        ImageGenerationService imageGenerationService,
        ICurrentUserService currentUser,
        ILogger<PortfolioPreviewService> logger)
    {
        _portfolioRepo = portfolioRepo;
        _previewRepo = previewRepo;
        _aiGenerator = aiGenerator;
        _visualPromptService = visualPromptService;
        _imageGenerationService = imageGenerationService;
        _currentUser = currentUser;
        _logger = logger;
    }

    async Task<Interfaces.GeneratePreviewResponse?> IPortfolioPreviewService.GeneratePreviewAsync(int portfolioId, string? highlightsDescription)
    {
        try
        {
            _logger.LogInformation("Generating preview for portfolio {PortfolioId}", portfolioId);

            // Get portfolio with blocks
            var portfolio = await _portfolioRepo.GetByIdAsync(portfolioId);
            if (portfolio == null)
            {
                _logger.LogWarning("❌ Portfolio {PortfolioId} not found", portfolioId);
                return new Interfaces.GeneratePreviewResponse 
                { 
                    Success = false, 
                    Message = $"Portfolio {portfolioId} not found" 
                };
            }

            // Check authorization (owner only)
            if (portfolio.EmployeeId != _currentUser.EmployeeId && !_currentUser.IsAdmin)
            {
                _logger.LogWarning("❌ Employee {EmployeeId} not authorized to generate preview for portfolio {PortfolioId}", 
                    _currentUser.EmployeeId, portfolioId);
                return new Interfaces.GeneratePreviewResponse 
                { 
                    Success = false, 
                    Message = "Not authorized to generate preview for this portfolio" 
                };
            }

            // Check portfolio is approved
            if (portfolio.ModerationStatus != "Approved")
            {
                _logger.LogWarning("⚠️ Portfolio {PortfolioId} not approved. Status: {Status}", portfolioId, portfolio.ModerationStatus);
                return new Interfaces.GeneratePreviewResponse 
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
                return new Interfaces.GeneratePreviewResponse 
                { 
                    Success = false, 
                    Message = $"Preview generation failed: {errorMessage}" 
                };
            }

            // Generate visual prompt (Phase 2)
            VisualPromptDto? visualPrompt = null;
            string? visualPromptJson = null;
            if (!string.IsNullOrEmpty(previewJson))
            {
                var (promptSuccess, prompt, promptError) = await _visualPromptService.GenerateVisualPromptAsync(
                    previewJson,
                    "professional",
                    null
                );

                if (promptSuccess && prompt != null)
                {
                    visualPrompt = prompt;
                    visualPromptJson = JsonSerializer.Serialize(prompt);
                    _logger.LogInformation("✅ Visual prompt generated successfully");
                }
                else
                {
                    _logger.LogWarning("⚠️ Visual prompt generation failed: {Error}", promptError);
                    visualPrompt = BuildFallbackVisualPrompt(previewJson, highlightsDescription);
                    visualPromptJson = JsonSerializer.Serialize(visualPrompt);
                    _logger.LogInformation("✅ Using fallback visual prompt for image generation");
                }
            }

            // Generate and upload image (Phase 3)
            string? imageUrl = null;
            string? imageId = null;
            if (visualPrompt != null)
            {
                var (imageSuccess, url, generatedImageId, imageError) = await _imageGenerationService.GenerateAndUploadImageAsync(
                    visualPrompt,
                    "professional"
                );

                if (imageSuccess && !string.IsNullOrEmpty(url))
                {
                    imageUrl = url;
                    imageId = generatedImageId;
                    _logger.LogInformation("✅ Image generated and uploaded: {Url}", imageUrl);
                }
                else
                {
                    _logger.LogWarning("⚠️ Image generation failed: {Error}", imageError);
                    // Don't fail the entire preview generation if image generation fails
                }
            }

            // Calculate cache key
            var cacheKey = CalculateCacheKey(previewJson, visualPromptJson);

            // Check if preview already exists
            var existingPreview = await _previewRepo.GetByPortfolioIdAsync(portfolioId);

            PortfolioPreview preview;
            if (existingPreview != null)
            {
                // Update existing preview
                existingPreview.PreviewJson = previewJson!;
                existingPreview.HighlightsDescription = highlightsDescription;
                existingPreview.VisualPrompt = visualPromptJson;
                existingPreview.ImageUrl = imageUrl;
                existingPreview.ImageId = imageId;
                existingPreview.SelectedTheme = "professional";
                existingPreview.ImagegenModel = "imagen-4.0-generate-001";
                existingPreview.GenerationModel = "gemini-2.5-flash";
                existingPreview.CacheKey = cacheKey;
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
                    VisualPrompt = visualPromptJson,
                    ImageUrl = imageUrl,
                    ImageId = imageId,
                    SelectedTheme = "professional",
                    ImagegenModel = "imagen-4.0-generate-001",
                    GenerationModel = "gemini-2.5-flash",
                    CacheKey = cacheKey,
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
            var response = MapToInterfaceResponse(preview, imageId);
            return new Interfaces.GeneratePreviewResponse 
            { 
                Success = true, 
                Message = "Preview generated successfully",
                Preview = response
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Unexpected error generating preview for portfolio {PortfolioId}", portfolioId);
            return new Interfaces.GeneratePreviewResponse 
            { 
                Success = false, 
                Message = $"Unexpected error: {ex.Message}" 
            };
        }
    }

    async Task<Interfaces.PortfolioPreviewResponse?> IPortfolioPreviewService.GetPreviewAsync(int portfolioId)
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
            if (!portfolio.IsPublic && portfolio.EmployeeId != _currentUser.EmployeeId && !_currentUser.IsAdmin)
            {
                _logger.LogWarning("Employee {EmployeeId} not authorized to view preview for portfolio {PortfolioId}", 
                    _currentUser.EmployeeId, portfolioId);
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
            return MapToInterfaceResponse(preview);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error retrieving preview for portfolio {PortfolioId}", portfolioId);
            return null;
        }
    }

    async Task<Interfaces.GeneratePreviewResponse?> IPortfolioPreviewService.RegeneratePreviewAsync(int portfolioId, string? newHighlights)
    {
        return await ((IPortfolioPreviewService)this).GeneratePreviewAsync(portfolioId, newHighlights);
    }

    /// <summary>
    /// Map PortfolioPreview entity to interface response DTO
    /// </summary>
    private Interfaces.PortfolioPreviewResponse MapToInterfaceResponse(PortfolioPreview preview, string? imageId = null)
    {
        var previewJson = ParsePreviewJson(preview.PreviewJson);

        return new Interfaces.PortfolioPreviewResponse
        {
            Id = preview.Id,
            PortfolioId = preview.PortfolioId,
            PreviewJson = previewJson,
            HighlightsDescription = preview.HighlightsDescription,
            ImageUrl = preview.ImageUrl,
            ImageId = imageId ?? preview.ImageId,
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

    /// <summary>
    /// Calculate cache key from preview and visual prompt
    /// </summary>
    private string CalculateCacheKey(string? previewJson, string? visualPromptJson)
    {
        var combined = $"{previewJson}|{visualPromptJson}";
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(combined));
        return Convert.ToBase64String(hash);
    }

    /// <summary>
    /// Build a deterministic fallback visual prompt when Gemini visual prompt generation is unavailable.
    /// </summary>
    private static VisualPromptDto BuildFallbackVisualPrompt(string previewJson, string? highlightsDescription)
    {
        string title = "Full-stack Developer";
        string summary = highlightsDescription ?? "Professional portfolio";
        string keySkills = "React, .NET, Azure";

        try
        {
            using var jsonDoc = JsonDocument.Parse(previewJson);
            var root = jsonDoc.RootElement;

            if (root.TryGetProperty("title", out var titleProp) && titleProp.ValueKind == JsonValueKind.String)
            {
                title = titleProp.GetString() ?? title;
            }

            if (root.TryGetProperty("summary", out var summaryProp) && summaryProp.ValueKind == JsonValueKind.String)
            {
                summary = summaryProp.GetString() ?? summary;
            }

            if (root.TryGetProperty("keySkills", out var skillsProp) && skillsProp.ValueKind == JsonValueKind.String)
            {
                keySkills = skillsProp.GetString() ?? keySkills;
            }
        }
        catch
        {
            // Use deterministic fallback values below.
        }

        return new VisualPromptDto
        {
            VisualTheme = "Professional portfolio showcase",
            MainElements = new List<string>
            {
                title,
                "skill badges",
                "project highlights",
                "Azure cloud accents"
            },
            ColorPalette = new List<string>
            {
                "#0F172A",
                "#2563EB",
                "#F8FAFC"
            },
            HeroText = title,
            Style = $"clean editorial layout emphasizing {keySkills} and the user's summary: {summary}"
        };
    }
}

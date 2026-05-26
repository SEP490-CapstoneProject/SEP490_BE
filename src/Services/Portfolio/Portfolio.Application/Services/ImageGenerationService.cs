using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Portfolio.Application.DTOs;

namespace Portfolio.Application.Services;

/// <summary>
/// Service for generating images using Cloudflare Workers AI
/// Generates images via Cloudflare Workers AI FLUX.1 Schnell model
/// - Free tier: 10,000 Neurons/day (~50-100 images)
/// Uses REST API endpoints with API token authentication
/// </summary>
public class ImageGenerationService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<ImageGenerationService> _logger;
    private readonly CloudflareImageService? _cloudflareImageService;

    public ImageGenerationService(
        IConfiguration configuration,
        ILogger<ImageGenerationService> logger,
        CloudflareImageService? cloudflareImageService = null)
    {
        _configuration = configuration;
        _logger = logger;
        _cloudflareImageService = cloudflareImageService;
    }

    /// <summary>
    /// Generate an image using Cloudflare Workers AI and upload to Media service
    /// Routes only to Cloudflare image generation (simplified from multi-provider)
    /// </summary>
    public async Task<(bool Success, string? ImageUrl, string? ImageId, string? ErrorMessage)> GenerateAndUploadImageAsync(
        VisualPromptDto visualPrompt,
        string selectedTheme = "professional",
        CancellationToken cancellationToken = default)
    {
        if (_cloudflareImageService == null)
        {
            return (false, null, null, "Cloudflare image service not configured");
        }

        _logger.LogInformation("🎨 Generating image via Cloudflare Workers AI for theme: {Theme}", selectedTheme);

        return await _cloudflareImageService.GenerateAndUploadImageAsync(visualPrompt, selectedTheme, cancellationToken);
    }


    #region Removed - Google Nano Banana Code Removed (Simplified to Cloudflare Only)
    // Removed GenerateWithGoogleAsync method
    // Removed GenerateImageWithGoogleAsync method
    // Removed UploadImageToMediaServiceAsync method (handled by CloudflareImageService)
    // Removed BuildGooglePrompt method
    // Removed NanoBananaResponse, Candidate, Content, Part, InlineData, UsageMetadata classes
    #endregion
}


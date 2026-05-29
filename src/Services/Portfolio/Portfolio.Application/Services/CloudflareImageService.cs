using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;

namespace Portfolio.Application.Services;

/// <summary>
/// Service for generating images using Cloudflare Workers AI with FLUX.1 Schnell model
/// Provides higher quota (10,000 Neurons/day ~50-100 images) vs Google Nano Banana (2 images/day)
/// Uses REST API endpoint with Bearer token authentication
/// </summary>
public class CloudflareImageService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IMediaServiceClient _mediaServiceClient;
    private readonly ILogger<CloudflareImageService> _logger;

    public CloudflareImageService(
        HttpClient httpClient,
        IConfiguration configuration,
        IMediaServiceClient mediaServiceClient,
        ILogger<CloudflareImageService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _mediaServiceClient = mediaServiceClient;
        _logger = logger;
    }

    /// <summary>
    /// Generate an image using Cloudflare Workers AI FLUX.1 Schnell and upload to Media service
    /// </summary>
    public async Task<(bool Success, string? ImageUrl, string? ImageId, string? ErrorMessage)> GenerateAndUploadImageAsync(
        VisualPromptDto visualPrompt,
        string selectedTheme = "professional",
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("🎨 Generating image via Cloudflare Workers AI for theme: {Theme}", selectedTheme);

            // Generate image using Cloudflare Workers AI
            var (imageSuccess, imageBytes, imageError) = await GenerateImageAsync(visualPrompt, cancellationToken);

            if (!imageSuccess || imageBytes == null || imageBytes.Length == 0)
            {
                _logger.LogError("❌ Cloudflare image generation failed: {Error}", imageError);
                return (false, null, null, imageError);
            }

            _logger.LogInformation("✅ Image generated via Cloudflare Workers AI ({SizeKB} KB)", imageBytes.Length / 1024);

            // Convert bytes to IFormFile and upload to Media service
            var (uploadSuccess, uploadUrl, uploadImageId, uploadError) = await UploadImageToMediaServiceAsync(
                imageBytes,
                selectedTheme,
                cancellationToken);

            if (!uploadSuccess || string.IsNullOrEmpty(uploadUrl))
            {
                _logger.LogError("❌ Image upload failed: {Error}", uploadError);
                return (false, null, null, uploadError);
            }

            _logger.LogInformation("✅ Image uploaded successfully: {Url}", uploadUrl);
            return (true, uploadUrl, uploadImageId, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Unexpected error in Cloudflare image generation pipeline");
            return (false, null, null, $"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>
    /// Call Cloudflare Workers AI FLUX.1 Schnell API to generate an image
    /// Uses REST API endpoint with Bearer token authentication
    /// </summary>
    private async Task<(bool Success, byte[]? ImageBytes, string? ErrorMessage)> GenerateImageAsync(
        VisualPromptDto visualPrompt,
        CancellationToken cancellationToken)
    {
        try
        {
            // Try multiple config key variations for robustness
            var accountId = _configuration["Cloudflare:AccountId"] 
                ?? throw new InvalidOperationException("Cloudflare AccountId not configured - check KeyVault or environment settings");
            
            var apiToken = _configuration["Cloudflare:ApiToken"] 
                ?? throw new InvalidOperationException("Cloudflare ApiToken not configured - check KeyVault or environment settings");
            
            if (string.IsNullOrEmpty(accountId) || accountId.Substring(0, 4) == "XXXX")
                throw new InvalidOperationException("Cloudflare AccountId not configured - check KeyVault or environment settings");
            
            if (string.IsNullOrEmpty(apiToken) || apiToken.StartsWith("XXXX"))
                throw new InvalidOperationException("Cloudflare ApiToken not configured - check KeyVault or environment settings");

            // Build prompt for FLUX.1 Schnell
            var prompt = BuildCloudflarePrompt(visualPrompt);

            _logger.LogInformation("📝 Calling Cloudflare Workers AI with prompt: {Prompt}", prompt);

            // Cloudflare Workers AI endpoint for FLUX.1 Schnell
            var requestUrl = $"https://api.cloudflare.com/client/v4/accounts/{accountId}/ai/run/@cf/black-forest-labs/flux-1-schnell";

            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Headers.Add("Authorization", $"Bearer {apiToken}");

            // Prepare request body for Cloudflare Workers AI
            request.Content = JsonContent.Create(new
            {
                prompt = prompt,
                num_steps = 20  // Balance between speed and quality (20 is good default)
            });

            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            _logger.LogInformation("🔄 Sending request to Cloudflare Workers AI endpoint...");
            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("❌ Cloudflare Workers AI API request failed: Status={Status}, Body={Body}", response.StatusCode, errorBody);
                return (false, null, $"Cloudflare API returned {response.StatusCode}: {errorBody}");
            }

            var payload = await response.Content.ReadFromJsonAsync<CloudflareImageResponse>(cancellationToken: cancellationToken);

            if (payload?.Success != true || payload.Result?.Image == null)
            {
                _logger.LogError("❌ No image from Cloudflare Workers AI API. Success={Success}, Message={Message}", 
                    payload?.Success, payload?.Errors?.FirstOrDefault()?.Message);
                return (false, null, "No image generated");
            }

            // Decode base64 image data from Cloudflare response
            byte[] imageBytes;
            try
            {
                imageBytes = Convert.FromBase64String(payload.Result.Image);
            }
            catch (FormatException ex)
            {
                _logger.LogError(ex, "❌ Failed to decode base64 image data from Cloudflare");
                return (false, null, "Invalid image format");
            }

            _logger.LogInformation("✅ Image generated from Cloudflare Workers AI: {SizeBytes} bytes", imageBytes.Length);
            return (true, imageBytes, null);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "❌ HTTP error calling Cloudflare Workers AI API");
            return (false, null, $"HTTP error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error calling Cloudflare Workers AI API");
            return (false, null, $"API error: {ex.Message}");
        }
    }

    /// <summary>
    /// Upload image bytes to Media service
    /// </summary>
    private async Task<(bool Success, string? Url, string? ImageId, string? ErrorMessage)> UploadImageToMediaServiceAsync(
        byte[] imageBytes,
        string theme,
        CancellationToken cancellationToken)
    {
        try
        {
            // Create IFormFile from bytes
            var fileName = $"portfolio-preview-{theme}-{DateTime.UtcNow:yyyyMMddHHmmss}.png";

            using var memoryStream = new MemoryStream(imageBytes);
            var formFile = new FormFile(memoryStream, 0, imageBytes.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = "image/png"
            };

            // Upload via Media service
            var (success, url, publicId) = await _mediaServiceClient.UploadImageAsync(formFile);

            if (!success || string.IsNullOrEmpty(url))
            {
                _logger.LogError("❌ Media service upload failed: Success={Success}, Url={Url}, PublicId={PublicId}", success, url, publicId);
                return (false, null, null, "Media service upload failed");
            }

            // Validate publicId is not empty
            if (string.IsNullOrEmpty(publicId))
            {
                _logger.LogWarning("⚠️ Media service returned empty PublicId. Url={Url}, will use filename as fallback", url);
                publicId = Path.GetFileNameWithoutExtension(fileName);
            }

            _logger.LogInformation("✅ Image uploaded to Media service: {Url}, PublicId: {PublicId}", url, publicId);
            return (true, url, publicId, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error uploading to Media service");
            return (false, null, null, $"Upload error: {ex.Message}");
        }
    }

    /// <summary>
    /// Build detailed prompt for FLUX.1 Schnell from visual prompt DTO with FIXED LAYOUT
    /// FLUX.1 Schnell works best with detailed, descriptive prompts
    /// </summary>
    private string BuildCloudflarePrompt(VisualPromptDto visualPrompt)
    {
        var elements = string.Join(", ", visualPrompt.MainElements ?? new List<string>());
        var colors = string.Join(", ", visualPrompt.ColorPalette ?? new List<string>());

        return $@"Professional portfolio preview image with FIXED LAYOUT, {visualPrompt.Style ?? "clean and modern"} art style.

Visual Theme: {visualPrompt.VisualTheme}

Key Elements: {elements}

Color Palette: {colors}

Hero Text/Title: {visualPrompt.HeroText}

MANDATORY FIXED LAYOUT (must be enforced):
- TOP-LEFT: Circular avatar photo (80x80px area with subtle border)
- TOP-RIGHT: Name/Title text (bold, large, professional)
- CENTER: Skills and technology badges (3-5 rounded pills with professional colors)
- BOTTOM: Achievement statement (centered, prominent, legible)

Layout Requirements:
- Avatar positioned EXACTLY at top-left corner in circular frame
- Name/Title positioned EXACTLY at top-right area
- Skills badges positioned in CENTER section horizontally arranged
- Achievement statement positioned at BOTTOM center
- Clean margins and balanced spacing throughout
- Professional, structured composition

Style Requirements:
- Professional, polished appearance
- High resolution, crystal clear and legible
- Balanced composition with strong visual hierarchy
- Prominently feature the hero text: '{visualPrompt.HeroText}'
- Use specified colors: {colors}
- Convey professional expertise and quality
- Modern, minimalist aesthetic
- Suitable for portfolio presentation and professional use
- No watermarks or extra text

Technical Specifications: 1920x1080px, HD quality, professional lighting, sharp details, web and print ready, legible text";
    }

    #region Cloudflare Workers AI Response Models

    private sealed class CloudflareImageResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("result")]
        public CloudflareImageResult? Result { get; set; }

        [JsonPropertyName("errors")]
        public List<CloudflareError>? Errors { get; set; }
    }

    private sealed class CloudflareImageResult
    {
        [JsonPropertyName("image")]
        public string? Image { get; set; }  // Base64 encoded image data
    }

    private sealed class CloudflareError
    {
        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("message")]
        public string? Message { get; set; }
    }

    #endregion
}

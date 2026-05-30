using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

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
        string? avatarBase64 = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("🎨 Generating image via Cloudflare Workers AI for theme: {Theme}", selectedTheme);

            // Generate image using Cloudflare Workers AI
            var (imageSuccess, imageBytes, imageError) = await GenerateImageAsync(visualPrompt, avatarBase64, cancellationToken);

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
        string? avatarBase64,
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

            if (!string.IsNullOrWhiteSpace(avatarBase64))
            {
                imageBytes = await OverlayAvatarAsync(imageBytes, avatarBase64, cancellationToken);
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
        var elements = LimitList(visualPrompt.MainElements, 4, 40);
        var colors = LimitList(visualPrompt.ColorPalette, 3, 20);
        var heroText = Truncate(visualPrompt.HeroText, 80);
        var style = Truncate(visualPrompt.Style, 120) ?? "clean and modern";

        var prompt = $@"Professional portfolio preview image, {style}.
Theme: {Truncate(visualPrompt.VisualTheme, 80)}
Hero text: {heroText}
Key elements: {elements}
Colors: {colors}
Layout: fixed 4-zone composition.
TOP-LEFT avatar, TOP-RIGHT name/title, CENTER skill badges, BOTTOM achievement.
Requirements: clean margins, balanced spacing, professional, legible, no watermarks.
Output: 1920x1080px, polished editorial design.";

        return prompt.Length <= 1800 ? prompt : BuildCompactCloudflarePrompt(heroText, colors);
    }

    private static string BuildCompactCloudflarePrompt(string heroText, string colors)
        => $@"Professional portfolio preview image. Hero text: {heroText}. Colors: {colors}. Fixed layout: top-left avatar, top-right title, center skill badges, bottom achievement. 1920x1080px. Clean, professional, legible, no watermarks.";

    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }

    private static string LimitList(IEnumerable<string>? items, int maxItems, int maxItemLength)
    {
        if (items == null)
        {
            return string.Empty;
        }

        return string.Join(", ", items
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Take(maxItems)
            .Select(x => Truncate(x, maxItemLength)));
    }

    private static async Task<byte[]> OverlayAvatarAsync(byte[] imageBytes, string avatarBase64, CancellationToken cancellationToken)
    {
        var avatarBytes = DecodeBase64Image(avatarBase64);

        using var baseImage = await Image.LoadAsync<Rgba32>(new MemoryStream(imageBytes), cancellationToken);
        using var avatarImage = await Image.LoadAsync<Rgba32>(new MemoryStream(avatarBytes), cancellationToken);

        avatarImage.Mutate(x => x.Resize(new ResizeOptions
        {
            Size = new Size(96, 96),
            Mode = ResizeMode.Crop
        }));

        baseImage.Mutate(x => x.DrawImage(avatarImage, new Point(32, 32), 1f));

        using var output = new MemoryStream();
        await baseImage.SaveAsPngAsync(output, cancellationToken);
        return output.ToArray();
    }

    private static byte[] DecodeBase64Image(string avatarBase64)
    {
        var cleaned = avatarBase64.Trim();
        var commaIndex = cleaned.IndexOf(',');
        if (cleaned.StartsWith("data:", StringComparison.OrdinalIgnoreCase) && commaIndex >= 0)
        {
            cleaned = cleaned[(commaIndex + 1)..];
        }

        return Convert.FromBase64String(cleaned);
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

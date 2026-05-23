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
/// Service for generating images using Google AI Studio Imagen 4 API
/// Uses direct REST API endpoint with API key authentication (no OAuth2 complexity)
/// </summary>
public class ImageGenerationService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IMediaServiceClient _mediaServiceClient;
    private readonly ILogger<ImageGenerationService> _logger;

    public ImageGenerationService(
        HttpClient httpClient,
        IConfiguration configuration,
        IMediaServiceClient mediaServiceClient,
        ILogger<ImageGenerationService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _mediaServiceClient = mediaServiceClient;
        _logger = logger;
    }

    /// <summary>
    /// Generate an image using Imagen 4 API and upload to Media service
    /// </summary>
    public async Task<(bool Success, string? ImageUrl, string? ImageId, string? ErrorMessage)> GenerateAndUploadImageAsync(
        VisualPromptDto visualPrompt,
        string selectedTheme = "professional",
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Generating image for theme: {Theme}", selectedTheme);

            // Generate image using Imagen 4
            var (imageSuccess, imageBytes, imageError) = await GenerateImageAsync(visualPrompt, cancellationToken);

            if (!imageSuccess || imageBytes == null || imageBytes.Length == 0)
            {
                _logger.LogError("❌ Image generation failed: {Error}", imageError);
                return (false, null, null, imageError);
            }

            _logger.LogInformation("✅ Image generated successfully ({SizeKB} KB)", imageBytes.Length / 1024);

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
            _logger.LogError(ex, "❌ Unexpected error in image generation pipeline");
            return (false, null, null, $"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>
    /// Call Google AI Studio Imagen 4 API to generate an image
    /// Uses REST API endpoint directly with API key authentication
    /// </summary>
    private async Task<(bool Success, byte[]? ImageBytes, string? ErrorMessage)> GenerateImageAsync(
        VisualPromptDto visualPrompt,
        CancellationToken cancellationToken)
    {
        try
        {
            var apiKey = _configuration["GoogleAI:ApiKey"] ?? throw new InvalidOperationException("GoogleAI ApiKey not configured");

            // Build a detailed prompt for Imagen
            var imagenPrompt = BuildImagenPrompt(visualPrompt);

            _logger.LogInformation("Calling Imagen 4 Generate API with prompt: {Prompt}", imagenPrompt);

            // Google AI Studio REST API endpoint for Imagen 4 Generate (API key only)
            var requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/imagen-4.0-generate-001:predict?key={apiKey}";

            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Headers.Add("x-goog-api-key", apiKey);

            // Prepare request body for Imagen 4 Generate via Google AI Studio
            request.Content = JsonContent.Create(new
            {
                instances = new[]
                {
                    new
                    {
                        prompt = imagenPrompt
                    }
                },
                parameters = new
                {
                    sampleCount = 1,
                    aspectRatio = "1:1"
                }
            });

            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("❌ Imagen API request failed: Status={Status}, Body={Body}", response.StatusCode, errorBody);
                return (false, null, $"Imagen API returned {response.StatusCode}");
            }

            var payload = await response.Content.ReadFromJsonAsync<ImagenGenerateResponse>(cancellationToken: cancellationToken);

            if (payload?.Predictions == null || payload.Predictions.Count == 0)
            {
                _logger.LogError("❌ No image predictions from Imagen API");
                return (false, null, "No images generated");
            }

            // Extract base64 image data
            var imageData = payload.Predictions[0].BytesBase64Encoded;
            if (string.IsNullOrEmpty(imageData))
            {
                _logger.LogError("❌ Empty image data from Imagen API");
                return (false, null, "Empty image data received");
            }

            // Decode base64 to bytes
            var imageBytes = Convert.FromBase64String(imageData);

            _logger.LogInformation("✅ Image generated from Imagen API: {SizeBytes} bytes", imageBytes.Length);
            return (true, imageBytes, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error calling Imagen API");
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
                _logger.LogError("❌ Media service upload failed");
                return (false, null, null, "Media service upload failed");
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
    /// Build detailed prompt for Imagen from visual prompt DTO
    /// </summary>
    private string BuildImagenPrompt(VisualPromptDto visualPrompt)
    {
        var elements = string.Join(", ", visualPrompt.MainElements ?? new List<string>());
        var colors = string.Join(", ", visualPrompt.ColorPalette ?? new List<string>());

        return $@"Professional portfolio preview image, {visualPrompt.Style ?? "clean and modern"} art style.

Visual Theme: {visualPrompt.VisualTheme}

Key Elements: {elements}

Color Palette: {colors}

Hero Text: ""{visualPrompt.HeroText}""

Style Directives:
- Professional quality, suitable for portfolio presentation
- High resolution, clear and legible
- Balanced composition with clear hierarchy
- Incorporate the hero text prominently
- Use the specified color palette
- Visual elements should convey expertise and quality
- Modern, clean design aesthetic
- Ready for web and print use

Technical: HD resolution, professional lighting, no watermarks, no text except hero text";
    }

    #region Imagen API Response Models

    private sealed class ImagenGenerateResponse
    {
        [JsonPropertyName("predictions")]
        public List<ImagePrediction>? Predictions { get; set; }
    }

    private sealed class ImagePrediction
    {
        [JsonPropertyName("bytesBase64Encoded")]
        public string? BytesBase64Encoded { get; set; }

        [JsonPropertyName("mimeType")]
        public string? MimeType { get; set; }
    }

    #endregion
}

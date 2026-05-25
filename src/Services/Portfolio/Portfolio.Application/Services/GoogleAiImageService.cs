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
/// Service for generating images using Google AI Studio Nano Banana (Gemini 2.5 Flash Image)
/// Limited to 2 images/day on free tier - provided as fallback when Cloudflare quota is exhausted
/// Uses REST API endpoint with API key authentication
/// </summary>
public class GoogleAiImageService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly IMediaServiceClient _mediaServiceClient;
    private readonly ILogger<GoogleAiImageService> _logger;

    public GoogleAiImageService(
        HttpClient httpClient,
        IConfiguration configuration,
        IMediaServiceClient mediaServiceClient,
        ILogger<GoogleAiImageService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _mediaServiceClient = mediaServiceClient;
        _logger = logger;
    }

    /// <summary>
    /// Generate an image using Google AI Nano Banana and upload to Media service
    /// </summary>
    public async Task<(bool Success, string? ImageUrl, string? ImageId, string? ErrorMessage)> GenerateAndUploadImageAsync(
        VisualPromptDto visualPrompt,
        string selectedTheme = "professional",
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("🔍 Generating image via Google AI Nano Banana for theme: {Theme}", selectedTheme);

            // Generate image using Nano Banana
            var (imageSuccess, imageBytes, imageError) = await GenerateImageAsync(visualPrompt, cancellationToken);

            if (!imageSuccess || imageBytes == null || imageBytes.Length == 0)
            {
                _logger.LogError("❌ Google AI image generation failed: {Error}", imageError);
                return (false, null, null, imageError);
            }

            _logger.LogInformation("✅ Image generated successfully via Google AI ({SizeKB} KB)", imageBytes.Length / 1024);

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
            _logger.LogError(ex, "❌ Unexpected error in Google AI image generation pipeline");
            return (false, null, null, $"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>
    /// Call Google AI Studio Nano Banana (Gemini 2.5 Flash Image) API to generate an image
    /// Uses REST API endpoint directly with API key authentication
    /// </summary>
    private async Task<(bool Success, byte[]? ImageBytes, string? ErrorMessage)> GenerateImageAsync(
        VisualPromptDto visualPrompt,
        CancellationToken cancellationToken)
    {
        try
        {
            var apiKey = _configuration["GoogleAI:ApiKey"] ?? throw new InvalidOperationException("GoogleAI ApiKey not configured");

            // Build a detailed prompt for Nano Banana
            var prompt = BuildPrompt(visualPrompt);

            _logger.LogInformation("📝 Calling Nano Banana API with prompt: {Prompt}", prompt);

            // Google AI Studio REST API endpoint for Nano Banana (gemini-2.5-flash-image)
            // This is the free-tier image generation model
            var requestUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash-image:generateContent?key={apiKey}";

            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            request.Headers.Add("x-goog-api-key", apiKey);

            // Prepare request body for Nano Banana (Gemini format)
            request.Content = JsonContent.Create(new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new[]
                        {
                            new
                            {
                                text = prompt
                            }
                        }
                    }
                },
                generationConfig = new
                {
                    temperature = 0.8f,
                    maxOutputTokens = 1024
                }
            });

            request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            _logger.LogInformation("🔄 Sending request to Google AI Nano Banana endpoint...");
            using var response = await _httpClient.SendAsync(request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("❌ Nano Banana API request failed: Status={Status}, Body={Body}", response.StatusCode, errorBody);
                return (false, null, $"Nano Banana API returned {response.StatusCode}");
            }

            var payload = await response.Content.ReadFromJsonAsync<NanoBananaResponse>(cancellationToken: cancellationToken);

            if (payload?.Candidates == null || payload.Candidates.Count == 0)
            {
                _logger.LogError("❌ No image candidates from Nano Banana API");
                return (false, null, "No images generated");
            }

            // Extract image data from Nano Banana response
            var candidate = payload.Candidates[0];
            var imagePart = candidate.Content?.Parts?.FirstOrDefault(p => p.InlineData != null);
            
            if (imagePart?.InlineData?.Data == null)
            {
                _logger.LogError("❌ Empty image data from Nano Banana API");
                return (false, null, "Empty image data received");
            }

            // Decode base64 to bytes
            var imageBytes = Convert.FromBase64String(imagePart.InlineData.Data);

            _logger.LogInformation("✅ Image generated from Nano Banana API: {SizeBytes} bytes", imageBytes.Length);
            return (true, imageBytes, null);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "❌ HTTP error calling Nano Banana API");
            return (false, null, $"HTTP error: {ex.Message}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error calling Nano Banana API");
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
    /// Build detailed prompt for Nano Banana from visual prompt DTO
    /// </summary>
    private string BuildPrompt(VisualPromptDto visualPrompt)
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

    #region Nano Banana API Response Models

    private sealed class NanoBananaResponse
    {
        [JsonPropertyName("candidates")]
        public List<Candidate>? Candidates { get; set; }

        [JsonPropertyName("usageMetadata")]
        public UsageMetadata? UsageMetadata { get; set; }
    }

    private sealed class Candidate
    {
        [JsonPropertyName("content")]
        public Content? Content { get; set; }
    }

    private sealed class Content
    {
        [JsonPropertyName("parts")]
        public List<Part>? Parts { get; set; }
    }

    private sealed class Part
    {
        [JsonPropertyName("inlineData")]
        public InlineData? InlineData { get; set; }

        [JsonPropertyName("text")]
        public string? Text { get; set; }
    }

    private sealed class InlineData
    {
        [JsonPropertyName("mimeType")]
        public string? MimeType { get; set; }

        [JsonPropertyName("data")]
        public string? Data { get; set; }
    }

    private sealed class UsageMetadata
    {
        [JsonPropertyName("inputTokenCount")]
        public int InputTokenCount { get; set; }

        [JsonPropertyName("outputTokenCount")]
        public int OutputTokenCount { get; set; }

        [JsonPropertyName("candidatesTokenCount")]
        public int CandidatesTokenCount { get; set; }
    }

    #endregion
}

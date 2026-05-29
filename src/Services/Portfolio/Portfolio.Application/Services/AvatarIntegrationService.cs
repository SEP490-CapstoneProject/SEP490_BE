using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Processing;

namespace Portfolio.Application.Services;

/// <summary>Service for downloading and converting portfolio avatars to base64 for image generation</summary>
public interface IAvatarIntegrationService
{
    /// <summary>Fetch avatar from URL and convert to base64 data URI</summary>
    Task<string?> FetchAndConvertAvatarToBase64Async(string? avatarUrl);

    /// <summary>Validate if avatar URL is accessible</summary>
    Task<bool> ValidateAvatarUrlAsync(string? avatarUrl);
}

public class AvatarIntegrationService : IAvatarIntegrationService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AvatarIntegrationService> _logger;
    private const int MaxAvatarSizeBytes = 2 * 1024 * 1024; // 2MB
    private const int StandardAvatarSize = 256; // 256x256 pixels

    public AvatarIntegrationService(
        HttpClient httpClient,
        ILogger<AvatarIntegrationService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>Fetch avatar from URL and convert to base64 data URI</summary>
    public async Task<string?> FetchAndConvertAvatarToBase64Async(string? avatarUrl)
    {
        if (string.IsNullOrWhiteSpace(avatarUrl))
        {
            _logger.LogDebug("Avatar URL is empty or null");
            return null;
        }

        try
        {
            // Validate URL
            if (!Uri.TryCreate(avatarUrl, UriKind.Absolute, out var uri))
            {
                _logger.LogWarning("Invalid avatar URL format: {AvatarUrl}", avatarUrl);
                return null;
            }

            // Download avatar with timeout
            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(5));
            var response = await _httpClient.GetAsync(uri, cts.Token);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to download avatar. Status: {StatusCode}", response.StatusCode);
                return null;
            }

            // Validate content type
            var contentType = response.Content.Headers.ContentType?.MediaType;
            if (contentType != "image/png" && contentType != "image/jpeg" && contentType != "image/jpg")
            {
                _logger.LogWarning("Invalid avatar content type: {ContentType}", contentType);
                return null;
            }

            // Check content length
            if (response.Content.Headers.ContentLength > MaxAvatarSizeBytes)
            {
                _logger.LogWarning("Avatar too large: {Size} bytes", response.Content.Headers.ContentLength);
                return null;
            }

            // Download image
            var imageBytes = await response.Content.ReadAsByteArrayAsync(cts.Token);

            if (imageBytes.Length > MaxAvatarSizeBytes)
            {
                _logger.LogWarning("Avatar size exceeds limit: {Size} bytes", imageBytes.Length);
                return null;
            }

            // Load, validate, and resize image
            using (var image = Image.Load(imageBytes))
            {
                // Validate image dimensions
                if (image.Width < 32 || image.Height < 32)
                {
                    _logger.LogWarning("Avatar too small: {Width}x{Height}px", image.Width, image.Height);
                    return null;
                }

                // Resize to standard size to reduce base64 string length
                var maxDimension = Math.Max(image.Width, image.Height);
                if (maxDimension > StandardAvatarSize)
                {
                    var scale = (double)StandardAvatarSize / maxDimension;
                    var newWidth = (int)(image.Width * scale);
                    var newHeight = (int)(image.Height * scale);

                    image.Mutate(ctx => ctx.Resize(newWidth, newHeight, KnownResamplers.Lanczos3));
                    _logger.LogDebug("Resized avatar to {Width}x{Height}px", image.Width, image.Height);
                }

                // Convert to base64 PNG
                using var ms = new MemoryStream();
                await image.SaveAsPngAsync(ms);
                var pngBytes = ms.ToArray();

                var base64 = Convert.ToBase64String(pngBytes);
                var dataUri = $"data:image/png;base64,{base64}";

                _logger.LogDebug("Successfully converted avatar to base64. Size: {Size} bytes", base64.Length);
                return dataUri;
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("Avatar download timeout");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error downloading and converting avatar from {AvatarUrl}", avatarUrl);
            return null;
        }
    }

    /// <summary>Validate if avatar URL is accessible</summary>
    public async Task<bool> ValidateAvatarUrlAsync(string? avatarUrl)
    {
        if (string.IsNullOrWhiteSpace(avatarUrl))
            return false;

        try
        {
            if (!Uri.TryCreate(avatarUrl, UriKind.Absolute, out var uri))
                return false;

            using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(3));
            var response = await _httpClient.GetAsync(uri, cts.Token);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Avatar validation failed for {AvatarUrl}", avatarUrl);
            return false;
        }
    }
}

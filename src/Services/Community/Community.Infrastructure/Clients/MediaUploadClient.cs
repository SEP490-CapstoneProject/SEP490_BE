using Community.Application.Clients;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Net.Http.Headers;

namespace Community.Infrastructure.Clients;

public class MediaUploadClient : IMediaUploadClient
{
    private readonly HttpClient _http;
    private readonly ILogger<MediaUploadClient> _logger;

    public MediaUploadClient(HttpClient http, ILogger<MediaUploadClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<MediaUploadResult?> UploadAsync(IFormFile file, string folder)
    {
        try
        {
            using var formData = new MultipartFormDataContent();
            using var stream = file.OpenReadStream();
            var fileContent = new StreamContent(stream);
            var contentType = ResolveContentType(file);
            if (!string.IsNullOrWhiteSpace(contentType))
            {
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            }
            formData.Add(fileContent, "file", file.FileName);

            var endpoint = contentType?.StartsWith("video/", StringComparison.OrdinalIgnoreCase) == true
                ? "/api/upload/video"
                : "/api/upload/image";
            endpoint = $"{endpoint}?folder={Uri.EscapeDataString(folder)}";
            var response = await _http.PostAsync(endpoint, formData);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Media upload failed for {FileName}: {StatusCode}. Body: {Body}",
                    file.FileName, response.StatusCode, errorBody);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<MediaServiceResponse>();
            if (result?.Success != true || string.IsNullOrEmpty(result.Url))
            {
                _logger.LogWarning("Media upload returned empty URL for {FileName}", file.FileName);
                return null;
            }

            return new MediaUploadResult { Url = result.Url, PublicId = result.PublicId };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file {FileName}", file.FileName);
            return null;
        }
    }

    private sealed class MediaServiceResponse
    {
        public bool Success { get; set; }
        public string? Url { get; set; }
        public string? PublicId { get; set; }
        public string? Message { get; set; }
        public string? Error { get; set; }
    }

    private static string? ResolveContentType(IFormFile file)
    {
        var contentType = file.ContentType;
        if (!string.IsNullOrWhiteSpace(contentType) &&
            !contentType.Equals("application/octet-stream", StringComparison.OrdinalIgnoreCase))
        {
            return contentType;
        }

        var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
        return ext switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".mp4" => "video/mp4",
            ".mpeg" => "video/mpeg",
            ".mov" => "video/quicktime",
            ".avi" => "video/x-msvideo",
            ".webm" => "video/webm",
            _ => contentType
        };
    }
}

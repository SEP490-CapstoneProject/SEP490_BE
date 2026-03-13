using Community.Application.Clients;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

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
            var formData = new MultipartFormDataContent();
            formData.Add(new StreamContent(file.OpenReadStream()), "file", file.FileName);
            formData.Add(new StringContent(folder), "folder");

            var endpoint = file.ContentType.StartsWith("video/") ? "/api/upload/video" : "/api/upload/image";
            var response = await _http.PostAsync(endpoint, formData);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Media upload failed for {FileName}: {StatusCode}", file.FileName, response.StatusCode);
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
}

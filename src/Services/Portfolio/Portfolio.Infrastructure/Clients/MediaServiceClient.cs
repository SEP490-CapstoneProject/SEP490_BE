using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;
using System.Net.Http.Json;

namespace Portfolio.Infrastructure.Clients;

public class MediaServiceClient : IMediaServiceClient
{
    private readonly HttpClient _http;
    private readonly ILogger<MediaServiceClient> _logger;

    public HttpClient Client => _http;

    public MediaServiceClient(HttpClient http, ILogger<MediaServiceClient> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<(bool Success, string Url, string PublicId)> UploadImageAsync(IFormFile file)
    {
        try
        {
            var form = new MultipartFormDataContent();
            var fileContent = new StreamContent(file.OpenReadStream());
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
            form.Add(fileContent, "file", file.FileName);

            var response = await _http.PostAsync("/api/upload/image", form);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Media upload failed: {Status}", response.StatusCode);
                return (false, string.Empty, string.Empty);
            }

            var result = await response.Content.ReadFromJsonAsync<MediaUploadResponse>();
            return result?.Success == true
                ? (true, result.Url ?? string.Empty, result.PublicId ?? string.Empty)
                : (false, string.Empty, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading to Media Service");
            return (false, string.Empty, string.Empty);
        }
    }

    public async Task<bool> DeleteMediaAsync(string publicId)
    {
        try
        {
            var encoded = Uri.EscapeDataString(publicId);
            var response = await _http.DeleteAsync($"/api/upload/{encoded}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting media {PublicId}", publicId);
            return false;
        }
    }
}

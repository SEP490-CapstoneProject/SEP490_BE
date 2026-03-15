using Company.Application.Clients;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace Company.Infrastructure.Clients;

public class MediaUploadClient : IMediaUploadClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MediaUploadClient> _logger;

    public MediaUploadClient(HttpClient httpClient, ILogger<MediaUploadClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<MediaUploadResult?> UploadAsync(IFormFile file, string folder)
    {
        try
        {
            using var form = new MultipartFormDataContent();
            using var stream = file.OpenReadStream();
            var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
            form.Add(fileContent, "file", file.FileName);
            form.Add(new StringContent(folder), "folder");

            var endpoint = file.ContentType.StartsWith("video/", StringComparison.OrdinalIgnoreCase)
                ? "/api/upload/video"
                : "/api/upload/image";

            var response = await _httpClient.PostAsync(endpoint, form);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Media upload failed: {Status} for {File}", response.StatusCode, file.FileName);
                return null;
            }

            var result = await response.Content.ReadFromJsonAsync<MediaUploadResult>();
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Exception uploading {File}", file.FileName);
            return null;
        }
    }
}

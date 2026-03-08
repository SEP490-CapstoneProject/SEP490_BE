using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Portfolio.Application.Interfaces;
using System.Net.Http.Json;

namespace Portfolio.Infrastructure.Services;

public class MediaService : IMediaService
{
    private readonly HttpClient _http;
    private readonly ILogger<MediaService> _logger;

    public MediaService(HttpClient http, ILogger<MediaService> logger)
    {
        _http = http;
        _logger = logger;
    }

    public async Task<string> SaveFileAsync(IFormFile file)
    {
        using var form = new MultipartFormDataContent();
        var stream = file.OpenReadStream();
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(file.ContentType);
        form.Add(fileContent, "file", file.FileName);

        var response = await _http.PostAsync("/api/upload/image", form);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<MediaUploadResult>();
        if (result?.Success != true || string.IsNullOrEmpty(result.Url))
            throw new InvalidOperationException($"Media upload failed for file: {file.FileName}");

        return result.Url;
    }

    private sealed class MediaUploadResult
    {
        public bool Success { get; init; }
        public string? Url { get; init; }
        public string? PublicId { get; init; }
    }
}

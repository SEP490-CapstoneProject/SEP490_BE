using Microsoft.AspNetCore.Http;

namespace Community.Application.Clients;

public interface IMediaUploadClient
{
    Task<MediaUploadResult?> UploadAsync(IFormFile file, string folder);
}

public class MediaUploadResult
{
    public string Url { get; set; } = string.Empty;
    public string? PublicId { get; set; }
}

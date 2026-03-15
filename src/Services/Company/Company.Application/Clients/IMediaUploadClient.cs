using Microsoft.AspNetCore.Http;

namespace Company.Application.Clients;

public interface IMediaUploadClient
{
    Task<MediaUploadResult?> UploadAsync(IFormFile file, string folder);
}

public class MediaUploadResult
{
    public string Url { get; set; } = "";
    public string? PublicId { get; set; }
}

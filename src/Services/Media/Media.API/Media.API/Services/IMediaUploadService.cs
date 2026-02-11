using Microsoft.AspNetCore.Http;

namespace Media.API.Services;

public interface IMediaUploadService
{
    Task<(bool success, string? url, string? publicId, string? error)> UploadImageAsync(IFormFile file, string folder = "uploads/images");
    Task<(bool success, string? url, string? publicId, string? error)> UploadVideoAsync(IFormFile file, string folder = "uploads/videos");
    Task<bool> DeleteMediaAsync(string publicId);
}

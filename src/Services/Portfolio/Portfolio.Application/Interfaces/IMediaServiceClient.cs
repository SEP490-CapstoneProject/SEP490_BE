using Microsoft.AspNetCore.Http;

namespace Portfolio.Application.Interfaces;

public interface IMediaServiceClient
{
    Task<(bool Success, string Url, string PublicId)> UploadImageAsync(IFormFile file);
    Task<bool> DeleteMediaAsync(string publicId);
}

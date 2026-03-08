using Microsoft.AspNetCore.Http;

namespace Portfolio.Application.Interfaces;

public interface IMediaService
{
    Task<string> SaveFileAsync(IFormFile file);
}

using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Media.API.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace Media.API.Services;

public class CloudinaryUploadService : IMediaUploadService
{
    private readonly Cloudinary _cloudinary;
    private readonly ILogger<CloudinaryUploadService> _logger;

    public CloudinaryUploadService(IOptions<CloudinarySettings> config, ILogger<CloudinaryUploadService> logger)
    {
        var account = new Account(
            config.Value.CloudName,
            config.Value.ApiKey,
            config.Value.ApiSecret
        );
        _cloudinary = new Cloudinary(account);
        _logger = logger;
    }

    public async Task<(bool success, string? url, string? publicId, string? error)> UploadImageAsync(
        IFormFile file, 
        string folder = "uploads/images")
    {
        try
        {
            if (file == null || file.Length == 0)
                return (false, null, null, "File is empty");

            // Validate file type
            var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp" };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
                return (false, null, null, "Invalid file type. Only JPEG, PNG, GIF, and WebP images are allowed.");

            // Validate file size (max 10MB)
            if (file.Length > 10 * 1024 * 1024)
                return (false, null, null, "File size exceeds 10MB limit");

            using var stream = file.OpenReadStream();
            
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder,
                Transformation = new Transformation()
                    .Quality("auto")
                    .FetchFormat("auto"),
                UseFilename = true,
                UniqueFilename = true
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                _logger.LogError("Cloudinary upload failed: {Error}", uploadResult.Error.Message);
                return (false, null, null, $"Upload failed: {uploadResult.Error.Message}");
            }

            _logger.LogInformation("Image uploaded successfully: {PublicId}", uploadResult.PublicId);
            return (true, uploadResult.SecureUrl.ToString(), uploadResult.PublicId, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading image");
            return (false, null, null, ex.Message);
        }
    }

    public async Task<(bool success, string? url, string? publicId, string? error)> UploadVideoAsync(
        IFormFile file, 
        string folder = "uploads/videos")
    {
        try
        {
            if (file == null || file.Length == 0)
                return (false, null, null, "File is empty");

            // Validate file type
            var allowedTypes = new[] { "video/mp4", "video/mpeg", "video/quicktime", "video/x-msvideo", "video/webm" };
            if (!allowedTypes.Contains(file.ContentType.ToLower()))
                return (false, null, null, "Invalid file type. Only MP4, MPEG, MOV, AVI, and WebM videos are allowed.");

            // Validate file size (max 100MB)
            if (file.Length > 100 * 1024 * 1024)
                return (false, null, null, "File size exceeds 100MB limit");

            using var stream = file.OpenReadStream();
            
            var uploadParams = new VideoUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = folder,
                Transformation = new Transformation()
                    .Quality("auto"),
                UseFilename = true,
                UniqueFilename = true
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                _logger.LogError("Cloudinary video upload failed: {Error}", uploadResult.Error.Message);
                return (false, null, null, $"Upload failed: {uploadResult.Error.Message}");
            }

            _logger.LogInformation("Video uploaded successfully: {PublicId}", uploadResult.PublicId);
            return (true, uploadResult.SecureUrl.ToString(), uploadResult.PublicId, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading video");
            return (false, null, null, ex.Message);
        }
    }

    public async Task<bool> DeleteMediaAsync(string publicId)
    {
        try
        {
            if (string.IsNullOrEmpty(publicId))
                return false;

            var deleteParams = new DeletionParams(publicId);
            var result = await _cloudinary.DestroyAsync(deleteParams);

            var success = result.Result == "ok";
            if (success)
                _logger.LogInformation("Media deleted successfully: {PublicId}", publicId);
            else
                _logger.LogWarning("Failed to delete media: {PublicId}", publicId);

            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting media: {PublicId}", publicId);
            return false;
        }
    }
}

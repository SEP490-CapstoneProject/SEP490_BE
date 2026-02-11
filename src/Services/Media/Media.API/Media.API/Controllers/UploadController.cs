using Microsoft.AspNetCore.Mvc;
using Media.API.Models;
using Media.API.Services;

namespace Media.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly IMediaUploadService _uploadService;
    private readonly ILogger<UploadController> _logger;

    public UploadController(IMediaUploadService uploadService, ILogger<UploadController> logger)
    {
        _uploadService = uploadService;
        _logger = logger;
    }

    /// <summary>
    /// Upload an image to Cloudinary
    /// </summary>
    /// <param name="file">Image file (JPEG, PNG, GIF, WebP - max 10MB)</param>
    /// <param name="folder">Optional folder path in Cloudinary (default: uploads/images)</param>
    /// <returns>Upload result with Cloudinary URL</returns>
    [HttpPost("image")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(UploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(UploadResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UploadResponse>> UploadImage(
        IFormFile file, 
        [FromQuery] string folder = "uploads/images")
    {
        _logger.LogInformation("Uploading image to folder: {Folder}", folder);

        var (success, url, publicId, error) = await _uploadService.UploadImageAsync(file, folder);

        if (!success)
        {
            return BadRequest(new UploadResponse
            {
                Success = false,
                Error = error
            });
        }

        return Ok(new UploadResponse
        {
            Success = true,
            Url = url,
            PublicId = publicId,
            Message = "Image uploaded successfully"
        });
    }

    /// <summary>
    /// Upload a video to Cloudinary
    /// </summary>
    /// <param name="file">Video file (MP4, MPEG, MOV, AVI, WebM - max 100MB)</param>
    /// <param name="folder">Optional folder path in Cloudinary (default: uploads/videos)</param>
    /// <returns>Upload result with Cloudinary URL</returns>
    [HttpPost("video")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(150 * 1024 * 1024)] // 150MB limit
    [ProducesResponseType(typeof(UploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(UploadResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<UploadResponse>> UploadVideo(
        IFormFile file, 
        [FromQuery] string folder = "uploads/videos")
    {
        _logger.LogInformation("Uploading video to folder: {Folder}", folder);

        var (success, url, publicId, error) = await _uploadService.UploadVideoAsync(file, folder);

        if (!success)
        {
            return BadRequest(new UploadResponse
            {
                Success = false,
                Error = error
            });
        }

        return Ok(new UploadResponse
        {
            Success = true,
            Url = url,
            PublicId = publicId,
            Message = "Video uploaded successfully"
        });
    }

    /// <summary>
    /// Delete media from Cloudinary
    /// </summary>
    /// <param name="publicId">Cloudinary public ID of the media to delete</param>
    /// <returns>Deletion result</returns>
    [HttpDelete("{publicId}")]
    [ProducesResponseType(typeof(UploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(UploadResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UploadResponse>> DeleteMedia(string publicId)
    {
        _logger.LogInformation("Deleting media: {PublicId}", publicId);

        var success = await _uploadService.DeleteMediaAsync(publicId);

        if (!success)
        {
            return NotFound(new UploadResponse
            {
                Success = false,
                Error = "Media not found or could not be deleted"
            });
        }

        return Ok(new UploadResponse
        {
            Success = true,
            Message = "Media deleted successfully"
        });
    }

    /// <summary>
    /// Health check endpoint
    /// </summary>
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(new { status = "healthy", service = "Media Upload Service" });
    }
}

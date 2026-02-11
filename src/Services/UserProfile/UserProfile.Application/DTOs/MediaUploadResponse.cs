namespace UserProfile.Application.DTOs;

public class MediaUploadResponse
{
    public bool Success { get; set; }
    public string? Url { get; set; }
    public string? PublicId { get; set; }
    public string? Message { get; set; }
    public string? Error { get; set; }
}

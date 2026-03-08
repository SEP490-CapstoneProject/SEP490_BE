using Microsoft.AspNetCore.Http;
using Portfolio.Application.Interfaces;
using Portfolio.Domain.Entities;
using System.Text.Json;

namespace Portfolio.Application.BlockHandlers;

public class IntroBlockHandler : IBlockHandler
{
    private readonly IMediaService _media;

    public IntroBlockHandler(IMediaService media) => _media = media;

    public string BlockType => "INTRO";

    public async Task HandleAsync(
        PortfolioBlock block,
        JsonElement data,
        Dictionary<string, IFormFile> files)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var request = data.Deserialize<IntroDataRequest>(options)
            ?? throw new ArgumentException("Invalid INTRO data");

        string? avatarUrl = null;
        if (!string.IsNullOrWhiteSpace(request.AvatarKey) && files.TryGetValue(request.AvatarKey, out var avatarFile))
            avatarUrl = await _media.SaveFileAsync(avatarFile);

        var result = new
        {
            avatar = avatarUrl,
            name = request.Name,
            studyField = request.StudyField,
            description = request.Description,
            email = request.Email,
            phone = request.Phone
        };

        block.DataJson = JsonSerializer.Serialize(result);
    }

    // DTO only used inside this handler
    private class IntroDataRequest
    {
        public string? AvatarKey { get; set; }
        public string? Name { get; set; }
        public string? StudyField { get; set; }
        public string? Description { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
    }
}

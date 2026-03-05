using Microsoft.AspNetCore.Http;
using Portfolio.Application.DTOs;
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
        Domain.Entities.Portfolio portfolio,
        PortfolioBlock block,
        JsonElement data,
        Dictionary<string, IFormFile> files)
    {
        var request = data.Deserialize<IntroDataRequest>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new ArgumentException("Invalid INTRO data");

        string? avatarUrl = null;
        if (!string.IsNullOrWhiteSpace(request.AvatarKey) && files.TryGetValue(request.AvatarKey, out var avatarFile))
            avatarUrl = await _media.SaveFileAsync(avatarFile);

        block.Intro = new Intro
        {
            PortfolioBlock = block,
            Avatar = avatarUrl,
            Name = request.Name,
            StudyField = request.StudyField,
            Description = request.Description,
            Email = request.Email,
            Phone = request.Phone
        };
    }
}

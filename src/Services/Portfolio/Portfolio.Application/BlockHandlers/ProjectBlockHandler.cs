using Microsoft.AspNetCore.Http;
using Portfolio.Application.Interfaces;
using Portfolio.Domain.Entities;
using System.Text.Json;

namespace Portfolio.Application.BlockHandlers;

public class ProjectBlockHandler : IBlockHandler
{
    private readonly IMediaService _media;

    public ProjectBlockHandler(IMediaService media) => _media = media;

    public string BlockType => "PROJECT";

    public async Task HandleAsync(
        PortfolioBlock block,
        JsonElement data,
        Dictionary<string, IFormFile> files)
    {
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var items = data.Deserialize<List<ProjectDataRequest>>(options) ?? new();

        var result = new List<object>();
        foreach (var p in items)
        {
            string? imageUrl = null;
            if (!string.IsNullOrWhiteSpace(p.ImageKey) && files.TryGetValue(p.ImageKey, out var imgFile))
                imageUrl = await _media.SaveFileAsync(imgFile);

            result.Add(new
            {
                name = p.Name,
                image = imageUrl,
                description = p.Description,
                role = p.Role,
                technology = p.Technology,
                links = p.Links.Select(l => new { type = l.Type, link = l.Link }).ToList()
            });
        }

        block.DataJson = JsonSerializer.Serialize(result);
    }

    private class ProjectDataRequest
    {
        public string? ImageKey { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Role { get; set; }
        public string? Technology { get; set; }
        public List<LinkRequest> Links { get; set; } = new();
    }

    private class LinkRequest
    {
        public string? Type { get; set; }
        public string? Link { get; set; }
    }
}

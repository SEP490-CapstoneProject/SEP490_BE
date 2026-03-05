using Microsoft.AspNetCore.Http;
using Portfolio.Application.DTOs;
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
        Domain.Entities.Portfolio portfolio,
        PortfolioBlock block,
        JsonElement data,
        Dictionary<string, IFormFile> files)
    {
        var items = data.Deserialize<List<ProjectDataRequest>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        foreach (var p in items)
        {
            string? imageUrl = null;
            if (!string.IsNullOrWhiteSpace(p.ImageKey) && files.TryGetValue(p.ImageKey, out var imgFile))
                imageUrl = await _media.SaveFileAsync(imgFile);

            var project = new Project
            {
                PortfolioBlock = block,
                Name = p.Name,
                Description = p.Description,
                Role = p.Role,
                Technology = p.Technology,
                Image = imageUrl
            };

            foreach (var link in p.Links)
                project.Links.Add(new ProjectLink { Project = project, Type = link.Type, Link = link.Link });

            block.Projects.Add(project);
        }
    }
}

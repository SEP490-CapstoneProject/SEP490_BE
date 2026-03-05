using Microsoft.AspNetCore.Http;
using Portfolio.Application.DTOs;
using Portfolio.Domain.Entities;
using System.Text.Json;

namespace Portfolio.Application.BlockHandlers;

public class AwardBlockHandler : IBlockHandler
{
    public string BlockType => "AWARD";

    public Task HandleAsync(
        Domain.Entities.Portfolio portfolio,
        PortfolioBlock block,
        JsonElement data,
        Dictionary<string, IFormFile> files)
    {
        var items = data.Deserialize<List<AwardDataRequest>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        foreach (var a in items)
            block.Awards.Add(new Award
            {
                PortfolioBlock = block,
                Name = a.Name,
                Date = a.Date,
                Organization = a.Organization,
                Description = a.Description
            });
        return Task.CompletedTask;
    }
}

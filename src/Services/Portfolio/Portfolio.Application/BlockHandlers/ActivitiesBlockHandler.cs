using Microsoft.AspNetCore.Http;
using Portfolio.Application.DTOs;
using Portfolio.Domain.Entities;
using System.Text.Json;

namespace Portfolio.Application.BlockHandlers;

public class ActivitiesBlockHandler : IBlockHandler
{
    public string BlockType => "ACTIVITIES";

    public Task HandleAsync(
        Domain.Entities.Portfolio portfolio,
        PortfolioBlock block,
        JsonElement data,
        Dictionary<string, IFormFile> files)
    {
        var items = data.Deserialize<List<ActivitiesDataRequest>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        foreach (var a in items)
            block.Activities.Add(new Activities
            {
                PortfolioBlock = block,
                Name = a.Name,
                Date = a.Date,
                Description = a.Description
            });
        return Task.CompletedTask;
    }
}

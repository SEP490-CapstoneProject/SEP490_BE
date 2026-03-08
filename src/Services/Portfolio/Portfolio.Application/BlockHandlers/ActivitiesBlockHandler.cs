using Microsoft.AspNetCore.Http;
using Portfolio.Domain.Entities;
using System.Text.Json;

namespace Portfolio.Application.BlockHandlers;

public class ActivitiesBlockHandler : IBlockHandler
{
    public string BlockType => "ACTIVITIES";

    public Task HandleAsync(PortfolioBlock block, JsonElement data, Dictionary<string, IFormFile> files)
    {
        block.DataJson = data.GetRawText();
        return Task.CompletedTask;
    }
}

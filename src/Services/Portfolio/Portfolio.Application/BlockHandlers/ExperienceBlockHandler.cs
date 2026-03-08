using Microsoft.AspNetCore.Http;
using Portfolio.Domain.Entities;
using System.Text.Json;

namespace Portfolio.Application.BlockHandlers;

public class ExperienceBlockHandler : IBlockHandler
{
    public string BlockType => "EXPERIMENT";

    public Task HandleAsync(PortfolioBlock block, JsonElement data, Dictionary<string, IFormFile> files)
    {
        block.DataJson = data.GetRawText();
        return Task.CompletedTask;
    }
}

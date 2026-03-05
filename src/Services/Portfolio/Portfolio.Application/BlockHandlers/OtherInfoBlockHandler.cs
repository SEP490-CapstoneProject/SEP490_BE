using Microsoft.AspNetCore.Http;
using Portfolio.Application.DTOs;
using Portfolio.Domain.Entities;
using System.Text.Json;

namespace Portfolio.Application.BlockHandlers;

public class OtherInfoBlockHandler : IBlockHandler
{
    public string BlockType => "OTHERINFO";

    public Task HandleAsync(
        Domain.Entities.Portfolio portfolio,
        PortfolioBlock block,
        JsonElement data,
        Dictionary<string, IFormFile> files)
    {
        var items = data.Deserialize<List<OtherInfoDataRequest>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        foreach (var o in items)
            block.OtherInfos.Add(new OtherInfo { PortfolioBlock = block, Detail = o.Detail });
        return Task.CompletedTask;
    }
}

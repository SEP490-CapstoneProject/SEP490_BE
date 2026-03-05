using Microsoft.AspNetCore.Http;
using Portfolio.Application.DTOs;
using Portfolio.Domain.Entities;
using System.Text.Json;

namespace Portfolio.Application.BlockHandlers;

public class SkillBlockHandler : IBlockHandler
{
    public string BlockType => "SKILL";

    public Task HandleAsync(
        Domain.Entities.Portfolio portfolio,
        PortfolioBlock block,
        JsonElement data,
        Dictionary<string, IFormFile> files)
    {
        var items = data.Deserialize<List<SkillDataRequest>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        foreach (var s in items)
            block.Skills.Add(new Skill { PortfolioBlock = block, Name = s.Name });
        return Task.CompletedTask;
    }
}

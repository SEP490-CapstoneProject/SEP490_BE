using Microsoft.AspNetCore.Http;
using Portfolio.Application.DTOs;
using Portfolio.Domain.Entities;
using System.Text.Json;

namespace Portfolio.Application.BlockHandlers;

public class DiplomaBlockHandler : IBlockHandler
{
    public string BlockType => "DIPLOMA";

    public Task HandleAsync(
        Domain.Entities.Portfolio portfolio,
        PortfolioBlock block,
        JsonElement data,
        Dictionary<string, IFormFile> files)
    {
        var items = data.Deserialize<List<DiplomaDataRequest>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        foreach (var d in items)
            block.Diplomas.Add(new Diploma
            {
                PortfolioBlock = block,
                Name = d.Name,
                Provider = d.Provider,
                Date = d.Date,
                Link = d.Link
            });
        return Task.CompletedTask;
    }
}

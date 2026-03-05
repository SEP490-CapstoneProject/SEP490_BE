using Microsoft.AspNetCore.Http;
using Portfolio.Application.DTOs;
using Portfolio.Domain.Entities;
using System.Text.Json;

namespace Portfolio.Application.BlockHandlers;

public class ReferenceBlockHandler : IBlockHandler
{
    public string BlockType => "REFERENCE";

    public Task HandleAsync(
        Domain.Entities.Portfolio portfolio,
        PortfolioBlock block,
        JsonElement data,
        Dictionary<string, IFormFile> files)
    {
        var items = data.Deserialize<List<ReferenceDataRequest>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        foreach (var r in items)
            block.References.Add(new Reference
            {
                PortfolioBlock = block,
                Name = r.Name,
                Position = r.Position,
                Mail = r.Mail,
                Phone = r.Phone
            });
        return Task.CompletedTask;
    }
}

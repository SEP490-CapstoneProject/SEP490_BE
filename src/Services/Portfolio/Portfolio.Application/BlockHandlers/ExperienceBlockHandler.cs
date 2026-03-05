using Microsoft.AspNetCore.Http;
using Portfolio.Application.DTOs;
using Portfolio.Domain.Entities;
using System.Text.Json;

namespace Portfolio.Application.BlockHandlers;

public class ExperienceBlockHandler : IBlockHandler
{
    public string BlockType => "EXPERIMENT";

    public Task HandleAsync(
        Domain.Entities.Portfolio portfolio,
        PortfolioBlock block,
        JsonElement data,
        Dictionary<string, IFormFile> files)
    {
        var items = data.Deserialize<List<ExperienceDataRequest>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        foreach (var e in items)
            block.Experiences.Add(new Experience
            {
                PortfolioBlock = block,
                JobName = e.JobName,
                Address = e.Address,
                StartDate = e.StartDate,
                EndDate = e.EndDate,
                Description = e.Description
            });
        return Task.CompletedTask;
    }
}

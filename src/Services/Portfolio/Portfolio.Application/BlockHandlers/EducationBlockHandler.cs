using Microsoft.AspNetCore.Http;
using Portfolio.Application.DTOs;
using Portfolio.Domain.Entities;
using System.Text.Json;

namespace Portfolio.Application.BlockHandlers;

public class EducationBlockHandler : IBlockHandler
{
    public string BlockType => "EDUCATION";

    public Task HandleAsync(
        Domain.Entities.Portfolio portfolio,
        PortfolioBlock block,
        JsonElement data,
        Dictionary<string, IFormFile> files)
    {
        var items = data.Deserialize<List<EducationDataRequest>>(new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();
        foreach (var e in items)
            block.Educations.Add(new Education
            {
                PortfolioBlock = block,
                SchoolName = e.SchoolName,
                Time = e.Time,
                Department = e.Department,
                Description = e.Description
            });
        return Task.CompletedTask;
    }
}

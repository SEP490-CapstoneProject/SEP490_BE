using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;
using Portfolio.Domain.Entities;

namespace Portfolio.Application.Services;

public class CriterionService : ICriterionService
{
    private readonly ICriterionRepository _repo;

    public CriterionService(ICriterionRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<CriterionDto>> GetAllAsync()
    {
        var list = await _repo.GetAllAsync();
        return list.Select(MapToDto).ToList();
    }

    public async Task<List<CriterionDto>> GetActiveAsync()
    {
        var list = await _repo.GetActiveAsync();
        return list.Select(MapToDto).ToList();
    }

    public async Task<CriterionDto> GetByIdAsync(int id)
    {
        var criterion = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Criterion {id} not found");
        return MapToDto(criterion);
    }

    public async Task<CriterionDto> CreateAsync(CreateCriterionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Name is required");

        if (string.IsNullOrWhiteSpace(request.Kind))
            throw new ArgumentException("Kind is required");

        var criterion = new Criterion
        {
            Name = request.Name.Trim(),
            Kind = request.Kind.Trim(),
            IsActive = true
        };

        var created = await _repo.CreateAsync(criterion);
        return MapToDto(created);
    }

    public async Task<CriterionDto> UpdateAsync(int id, UpdateCriterionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ArgumentException("Name is required");

        if (string.IsNullOrWhiteSpace(request.Kind))
            throw new ArgumentException("Kind is required");

        var criterion = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Criterion {id} not found");

        criterion.Name = request.Name.Trim();
        criterion.Kind = request.Kind.Trim();

        var updated = await _repo.UpdateAsync(criterion);
        return MapToDto(updated);
    }

    public async Task<CriterionDto> ToggleActiveAsync(int id)
    {
        var criterion = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Criterion {id} not found");

        criterion.IsActive = !criterion.IsActive;

        var updated = await _repo.UpdateAsync(criterion);
        return MapToDto(updated);
    }

    public async Task DeleteAsync(int id)
    {
        var criterion = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Criterion {id} not found");

        // Soft-delete via deactivation to preserve data integrity
        criterion.IsActive = false;
        await _repo.UpdateAsync(criterion);
    }

    private static CriterionDto MapToDto(Criterion c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Kind = c.Kind,
        IsActive = c.IsActive
    };
}

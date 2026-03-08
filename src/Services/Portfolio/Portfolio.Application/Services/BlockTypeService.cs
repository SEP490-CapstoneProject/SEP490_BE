using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;
using Portfolio.Domain.Entities;

namespace Portfolio.Application.Services;

public class BlockTypeService : IBlockTypeService
{
    private readonly IBlockTypeRepository _repo;

    public BlockTypeService(IBlockTypeRepository repo)
    {
        _repo = repo;
    }

    public async Task<List<BlockTypeDto>> GetAllAsync()
    {
        var list = await _repo.GetAllAsync();
        return list.Select(MapToDto).ToList();
    }

    public async Task<List<BlockTypeDto>> GetActiveAsync()
    {
        var list = await _repo.GetActiveAsync();
        return list.Select(MapToDto).ToList();
    }

    public async Task<BlockTypeDto> CreateAsync(CreateBlockTypeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            throw new ArgumentException("Code is required");

        var code = request.Code.Trim().ToUpperInvariant();

        var existing = await _repo.GetByCodeAsync(code);
        if (existing != null)
            throw new InvalidOperationException($"BlockType with code '{code}' already exists");

        var blockType = new BlockType
        {
            Code = code,
            IsMultiple = request.IsMultiple,
            IsActive = true
        };

        var created = await _repo.CreateAsync(blockType);
        return MapToDto(created);
    }

    public async Task<BlockTypeDto> UpdateAsync(int id, UpdateBlockTypeRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            throw new ArgumentException("Code is required");

        var blockType = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"BlockType {id} not found");

        var code = request.Code.Trim().ToUpperInvariant();

        // Check for duplicate code (only if code changed)
        if (!string.Equals(blockType.Code, code, StringComparison.OrdinalIgnoreCase))
        {
            var existing = await _repo.GetByCodeAsync(code);
            if (existing != null)
                throw new InvalidOperationException($"BlockType with code '{code}' already exists");
        }

        blockType.Code = code;
        blockType.IsMultiple = request.IsMultiple;

        var updated = await _repo.UpdateAsync(blockType);
        return MapToDto(updated);
    }

    public async Task<BlockTypeDto> ToggleActiveAsync(int id)
    {
        var blockType = await _repo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"BlockType {id} not found");

        blockType.IsActive = !blockType.IsActive;

        var updated = await _repo.UpdateAsync(blockType);
        return MapToDto(updated);
    }

    private static BlockTypeDto MapToDto(BlockType bt) => new()
    {
        Id = bt.Id,
        Code = bt.Code,
        IsMultiple = bt.IsMultiple,
        IsActive = bt.IsActive
    };
}

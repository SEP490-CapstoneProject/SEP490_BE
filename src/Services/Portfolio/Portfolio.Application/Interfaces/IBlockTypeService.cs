using Portfolio.Application.DTOs;

namespace Portfolio.Application.Interfaces;

public interface IBlockTypeService
{
    Task<List<BlockTypeDto>> GetAllAsync();
    Task<List<BlockTypeDto>> GetActiveAsync();
    Task<BlockTypeDto> CreateAsync(CreateBlockTypeRequest request);
    Task<BlockTypeDto> UpdateAsync(int id, UpdateBlockTypeRequest request);
    Task<BlockTypeDto> ToggleActiveAsync(int id);
}

using Portfolio.Domain.Entities;

namespace Portfolio.Application.Interfaces;

public interface IBlockTypeRepository
{
    Task<List<BlockType>> GetAllAsync();
    Task<List<BlockType>> GetActiveAsync();
    Task<BlockType?> GetByIdAsync(int id);
    Task<BlockType?> GetByCodeAsync(string code);
    Task<BlockType> CreateAsync(BlockType blockType);
    Task<BlockType> UpdateAsync(BlockType blockType);
}

using Microsoft.AspNetCore.Http;
using Portfolio.Application.DTOs;

namespace Portfolio.Application.Interfaces;

public interface IBlockService
{
    Task<BlockDto> AddBlockAsync(int portfolioId, int employeeId, AddBlockRequest request, IFormFile? avatarFile, IFormFile? projectImageFile);
    Task<BlockDto> UpdateBlockAsync(int blockId, int portfolioId, int employeeId, UpdateBlockRequest request, IFormFile? avatarFile, IFormFile? projectImageFile);
    Task DeleteBlockAsync(int blockId, int portfolioId, int employeeId);
    Task ReorderBlocksAsync(int portfolioId, int employeeId, ReorderBlocksRequest request);
}

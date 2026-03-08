using Microsoft.AspNetCore.Http;
using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;
using Portfolio.Domain.Entities;
using System.Text.Json;

namespace Portfolio.Application.Interfaces;

public interface IBlockService
{
    Task<BlockDto> AddBlockAsync(int portfolioId, int employeeId, AddBlockRequest request, Dictionary<string, IFormFile> fileMap);
    Task<BlockDto> UpdateBlockAsync(int blockId, int portfolioId, int employeeId, UpdateBlockRequest request, Dictionary<string, IFormFile> fileMap);
    Task DeleteBlockAsync(int blockId, int portfolioId, int employeeId);
    Task ReorderBlocksAsync(int portfolioId, int employeeId, ReorderBlocksRequest request);
    BlockDto MapBlockToDto(PortfolioBlock block);
}

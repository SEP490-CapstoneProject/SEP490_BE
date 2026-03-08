using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Portfolio.Application.BlockHandlers;
using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;
using Portfolio.Domain.Entities;
using System.Text.Json;

namespace Portfolio.Application.Services;

public class BlockService : IBlockService
{
    private readonly IBlockRepository _blockRepo;
    private readonly IPortfolioRepository _portfolioRepo;
    private readonly IEnumerable<IBlockHandler> _handlers;
    private readonly ILogger<BlockService> _logger;

    public BlockService(
        IBlockRepository blockRepo,
        IPortfolioRepository portfolioRepo,
        IEnumerable<IBlockHandler> handlers,
        ILogger<BlockService> logger)
    {
        _blockRepo = blockRepo;
        _portfolioRepo = portfolioRepo;
        _handlers = handlers;
        _logger = logger;
    }

    public async Task<BlockDto> AddBlockAsync(int portfolioId, int employeeId, AddBlockRequest request, Dictionary<string, IFormFile> fileMap)
    {
        var portfolio = await _portfolioRepo.GetByIdAsync(portfolioId)
            ?? throw new KeyNotFoundException($"Portfolio {portfolioId} not found");

        if (portfolio.EmployeeId != employeeId)
            throw new UnauthorizedAccessException("You do not own this portfolio");

        var blockTypeInfo = GetBlockTypeInfo(request.BlockTypeCode);

        if (!blockTypeInfo.IsMultiple)
        {
            var count = await _blockRepo.CountByTypeAsync(portfolioId, blockTypeInfo.Id);
            if (count >= 1)
                throw new InvalidOperationException($"Block type '{request.BlockTypeCode}' allows only one per portfolio");
        }

        var order = request.DisplayOrder ?? (await _blockRepo.GetMaxOrderAsync(portfolioId) + 1);

        var block = new PortfolioBlock
        {
            PortfolioId = portfolioId,
            BlockTypeId = blockTypeInfo.Id,
            Variant = request.Variant,
            DisplayOrder = order,
            IsVisible = true
        };

        var handlerMap = _handlers.ToDictionary(h => h.BlockType, StringComparer.OrdinalIgnoreCase);
        if (handlerMap.TryGetValue(request.BlockTypeCode, out var handler))
            await handler.HandleAsync(block, request.Data, fileMap);

        var created = await _blockRepo.CreateAsync(block);
        var full = await _blockRepo.GetByIdWithDataAsync(created.Id);
        return MapBlockToDto(full!);
    }

    public async Task<BlockDto> UpdateBlockAsync(int blockId, int portfolioId, int employeeId, UpdateBlockRequest request, Dictionary<string, IFormFile> fileMap)
    {
        var portfolio = await _portfolioRepo.GetByIdAsync(portfolioId)
            ?? throw new KeyNotFoundException($"Portfolio {portfolioId} not found");

        if (portfolio.EmployeeId != employeeId)
            throw new UnauthorizedAccessException("You do not own this portfolio");

        var block = await _blockRepo.GetByIdWithDataAsync(blockId)
            ?? throw new KeyNotFoundException($"Block {blockId} not found");

        if (block.PortfolioId != portfolioId)
            throw new InvalidOperationException("Block does not belong to this portfolio");

        block.Variant = request.Variant;
        block.IsVisible = request.IsVisible;

        var handlerMap = _handlers.ToDictionary(h => h.BlockType, StringComparer.OrdinalIgnoreCase);
        if (handlerMap.TryGetValue(block.BlockType.Code, out var handler))
            await handler.HandleAsync(block, request.Data, fileMap);

        await _blockRepo.UpdateAsync(block);

        var full = await _blockRepo.GetByIdWithDataAsync(blockId);
        return MapBlockToDto(full!);
    }

    public async Task DeleteBlockAsync(int blockId, int portfolioId, int employeeId)
    {
        var portfolio = await _portfolioRepo.GetByIdAsync(portfolioId)
            ?? throw new KeyNotFoundException($"Portfolio {portfolioId} not found");

        if (portfolio.EmployeeId != employeeId)
            throw new UnauthorizedAccessException("You do not own this portfolio");

        var block = await _blockRepo.GetByIdWithDataAsync(blockId)
            ?? throw new KeyNotFoundException($"Block {blockId} not found");

        if (block.PortfolioId != portfolioId)
            throw new InvalidOperationException("Block does not belong to this portfolio");

        await _blockRepo.DeleteAsync(block);
    }

    public async Task ReorderBlocksAsync(int portfolioId, int employeeId, ReorderBlocksRequest request)
    {
        var portfolio = await _portfolioRepo.GetByIdAsync(portfolioId)
            ?? throw new KeyNotFoundException($"Portfolio {portfolioId} not found");

        if (portfolio.EmployeeId != employeeId)
            throw new UnauthorizedAccessException("You do not own this portfolio");

        var reorders = request.Items.Select(i => (i.BlockId, i.DisplayOrder)).ToList();
        await _blockRepo.ReorderAsync(reorders);
    }

    public BlockDto MapBlockToDto(PortfolioBlock block)
    {
        var code = block.BlockType?.Code?.ToUpper() ?? string.Empty;
        object data = string.IsNullOrEmpty(block.DataJson) || block.DataJson == "{}"
            ? new object()
            : JsonSerializer.Deserialize<JsonElement>(block.DataJson);

        return new BlockDto
        {
            Id = block.Id,
            Type = code,
            Variant = block.Variant,
            Order = block.DisplayOrder,
            Data = data
        };
    }

    private static (int Id, bool IsMultiple) GetBlockTypeInfo(string code) => code.ToUpper() switch
    {
        "INTRO"      => (1, false),
        "SKILL"      => (2, true),
        "EDUCATION"  => (3, true),
        "DIPLOMA"    => (4, true),
        "EXPERIMENT" => (5, true),
        "PROJECT"    => (6, true),
        "AWARD"      => (7, true),
        "ACTIVITIES" => (8, true),
        "OTHERINFO"  => (9, true),
        "REFERENCE"  => (10, true),
        _ => throw new ArgumentException($"Unknown block type: {code}")
    };
}
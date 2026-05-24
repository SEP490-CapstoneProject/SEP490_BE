using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace Portfolio.Application.DTOs;

// ─── Portfolio Response ───────────────────────────────────────────────────────

public class PortfolioDto
{
    public int PortfolioId { get; set; }
    public int EmployeeId { get; set; }
    public string PortfolioName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ModerationStatus { get; set; } = "PendingReview";
    public string? ModerationReason { get; set; }
    public DateTime? ModeratedAt { get; set; }
    public bool IsMain { get; set; }
    public bool IsPublic { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsFollowed { get; set; }
    public RankingDto Ranking { get; set; } = new();
    public List<PortfolioReviewerDto> Reviewers { get; set; } = new();
    public List<BlockDto> Blocks { get; set; } = new();
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)Total / PageSize) : 0;
}

/// <summary>
/// Lightweight portfolio summary used for matching result enrichment.
/// </summary>
public class PortfolioSummaryDto
{
    public int PortfolioId { get; set; }
    public int EmployeeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ModerationStatus { get; set; } = string.Empty;
    public bool IsMain { get; set; }
    public bool IsPublic { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<BlockDto> Blocks { get; set; } = new();
}


public class UpdatePortfolioRequest
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "active";
    public bool? IsMain { get; set; }
    public bool? IsPublic { get; set; }
}

public class ApprovePortfolioRequest
{
    public string? Notes { get; set; }
}

public class RejectPortfolioRequest
{
    public string Reason { get; set; } = string.Empty;
}

// ─── Block Response ───────────────────────────────────────────────────────────

/// <summary>
/// Represents one block in a portfolio.
/// data is object for INTRO (1-1), array for all other types.
/// </summary>
public class BlockDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;  // BlockType.Code
    public string Variant { get; set; } = string.Empty;
    public int Order { get; set; }
    public object Data { get; set; } = new(); // object for INTRO, List<T> for others
}

// ─── Block Requests ───────────────────────────────────────────────────────────

public class AddBlockRequest
{
    public string BlockTypeCode { get; set; } = string.Empty;
    public string Variant { get; set; } = string.Empty;
    public int? DisplayOrder { get; set; }
    public JsonElement Data { get; set; }
}

public class UpdateBlockRequest
{
    public string Variant { get; set; } = string.Empty;
    public bool IsVisible { get; set; } = true;
    public JsonElement Data { get; set; }
}

public class ReorderBlocksRequest
{
    public List<BlockOrderItem> Items { get; set; } = new();
}

public class BlockOrderItem
{
    public int BlockId { get; set; }
    public int DisplayOrder { get; set; }
}

public class CreateFullPortfolioFormRequest
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "active";

    public string BlocksJson { get; set; } = string.Empty;

    public List<IFormFile>? Files { get; set; }
}

public class FullBlockImportRequest
{
    public string BlockTypeCode { get; set; } = string.Empty;
    public string Variant { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }

    public string? FileKey { get; set; }

    public JsonElement Data { get; set; }
}

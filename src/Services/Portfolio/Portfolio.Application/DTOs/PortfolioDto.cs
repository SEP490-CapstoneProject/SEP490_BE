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
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<BlockDto> Blocks { get; set; } = new();
}

public class UpdatePortfolioRequest
{
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "active";
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
    public string BlockTypeCode { get; set; } = string.Empty; // e.g. "INTRO", "SKILL"
    public string Variant { get; set; } = string.Empty;
    public int? DisplayOrder { get; set; }

    // Block type-specific data (only one of these should be populated)
    public IntroDataRequest? IntroData { get; set; }
    public List<SkillDataRequest>? SkillData { get; set; }
    public EducationDataRequest? EducationData { get; set; }
    public DiplomaDataRequest? DiplomaData { get; set; }
    public ExperienceDataRequest? ExperienceData { get; set; }
    public ProjectDataRequest? ProjectData { get; set; }
    public AwardDataRequest? AwardData { get; set; }
    public ActivitiesDataRequest? ActivitiesData { get; set; }
    public OtherInfoDataRequest? OtherInfoData { get; set; }
    public ReferenceDataRequest? ReferenceData { get; set; }
}

public class UpdateBlockRequest
{
    public string Variant { get; set; } = string.Empty;
    public bool IsVisible { get; set; } = true;

    // Block type-specific data
    public IntroDataRequest? IntroData { get; set; }
    public List<SkillDataRequest>? SkillData { get; set; }
    public EducationDataRequest? EducationData { get; set; }
    public DiplomaDataRequest? DiplomaData { get; set; }
    public ExperienceDataRequest? ExperienceData { get; set; }
    public ProjectDataRequest? ProjectData { get; set; }
    public AwardDataRequest? AwardData { get; set; }
    public ActivitiesDataRequest? ActivitiesData { get; set; }
    public OtherInfoDataRequest? OtherInfoData { get; set; }
    public ReferenceDataRequest? ReferenceData { get; set; }
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

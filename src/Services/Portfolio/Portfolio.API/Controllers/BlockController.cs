using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;

namespace Portfolio.API.Controllers;

[ApiController]
[Route("api/portfolio/{portfolioId:int}/blocks")]
[Authorize]
public class BlockController : ControllerBase
{
    private readonly IBlockService _blockService;
    private readonly ILogger<BlockController> _logger;

    public BlockController(IBlockService blockService, ILogger<BlockController> logger)
    {
        _blockService = blockService;
        _logger = logger;
    }

    // ─── ADD Block ────────────────────────────────────────────────────────────

    /// <summary>
    /// Add a block to portfolio. Use multipart/form-data to send JSON data + optional image files.
    /// </summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> AddBlock(
        int portfolioId,
        [FromForm] string blockTypeCode,
        [FromForm] string variant,
        [FromForm] int? displayOrder,
        // Intro
        [FromForm] string? introName,
        [FromForm] string? introStudyField,
        [FromForm] string? introDescription,
        [FromForm] string? introEmail,
        [FromForm] string? introPhone,
        IFormFile? avatarFile,
        // Skill (comma-separated names)
        [FromForm] string? skillNames,
        // Education
        [FromForm] string? educationSchoolName,
        [FromForm] string? educationTime,
        [FromForm] string? educationDepartment,
        [FromForm] string? educationDescription,
        // Diploma
        [FromForm] string? diplomaName,
        [FromForm] string? diplomaProvider,
        [FromForm] string? diplomaDate,
        [FromForm] string? diplomaLink,
        // Experience
        [FromForm] string? experienceJobName,
        [FromForm] string? experienceAddress,
        [FromForm] string? experienceStartDate,
        [FromForm] string? experienceEndDate,
        [FromForm] string? experienceDescription,
        // Project
        [FromForm] string? projectName,
        [FromForm] string? projectDescription,
        [FromForm] string? projectRole,
        [FromForm] string? projectTechnology,
        [FromForm] string? projectLinksJson, // JSON array of {type, link}
        IFormFile? projectImageFile,
        // Award
        [FromForm] string? awardName,
        [FromForm] string? awardDate,
        [FromForm] string? awardOrganization,
        [FromForm] string? awardDescription,
        // Activities
        [FromForm] string? activitiesName,
        [FromForm] string? activitiesDate,
        [FromForm] string? activitiesDescription,
        // OtherInfo
        [FromForm] string? otherInfoDetail,
        // Reference
        [FromForm] string? referenceName,
        [FromForm] string? referencePosition,
        [FromForm] string? referenceMail,
        [FromForm] string? referencePhone)
    {
        var employeeId = GetEmployeeId();
        if (employeeId == null) return Unauthorized(new { error = "EmployeeId claim not found" });

        var request = BuildAddRequest(blockTypeCode, variant, displayOrder,
            introName, introStudyField, introDescription, introEmail, introPhone,
            skillNames,
            educationSchoolName, educationTime, educationDepartment, educationDescription,
            diplomaName, diplomaProvider, diplomaDate, diplomaLink,
            experienceJobName, experienceAddress, experienceStartDate, experienceEndDate, experienceDescription,
            projectName, projectDescription, projectRole, projectTechnology, projectLinksJson,
            awardName, awardDate, awardOrganization, awardDescription,
            activitiesName, activitiesDate, activitiesDescription,
            otherInfoDetail,
            referenceName, referencePosition, referenceMail, referencePhone);

        try
        {
            var block = await _blockService.AddBlockAsync(portfolioId, employeeId.Value, request, avatarFile, projectImageFile);
            return CreatedAtRoute(null, block);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (InvalidOperationException ex) { return Conflict(new { error = ex.Message }); }
        catch (ArgumentException ex) { return BadRequest(new { error = ex.Message }); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding block to portfolio {PortfolioId}", portfolioId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    // ─── UPDATE Block ─────────────────────────────────────────────────────────

    /// <summary>Update block metadata and data content</summary>
    [HttpPut("{blockId:int}")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> UpdateBlock(
        int portfolioId,
        int blockId,
        [FromForm] string variant,
        [FromForm] bool isVisible = true,
        [FromForm] string? introName = null,
        [FromForm] string? introStudyField = null,
        [FromForm] string? introDescription = null,
        [FromForm] string? introEmail = null,
        [FromForm] string? introPhone = null,
        IFormFile? avatarFile = null,
        [FromForm] string? skillNames = null,
        [FromForm] string? educationSchoolName = null,
        [FromForm] string? educationTime = null,
        [FromForm] string? educationDepartment = null,
        [FromForm] string? educationDescription = null,
        [FromForm] string? diplomaName = null,
        [FromForm] string? diplomaProvider = null,
        [FromForm] string? diplomaDate = null,
        [FromForm] string? diplomaLink = null,
        [FromForm] string? experienceJobName = null,
        [FromForm] string? experienceAddress = null,
        [FromForm] string? experienceStartDate = null,
        [FromForm] string? experienceEndDate = null,
        [FromForm] string? experienceDescription = null,
        [FromForm] string? projectName = null,
        [FromForm] string? projectDescription = null,
        [FromForm] string? projectRole = null,
        [FromForm] string? projectTechnology = null,
        [FromForm] string? projectLinksJson = null,
        IFormFile? projectImageFile = null,
        [FromForm] string? awardName = null,
        [FromForm] string? awardDate = null,
        [FromForm] string? awardOrganization = null,
        [FromForm] string? awardDescription = null,
        [FromForm] string? activitiesName = null,
        [FromForm] string? activitiesDate = null,
        [FromForm] string? activitiesDescription = null,
        [FromForm] string? otherInfoDetail = null,
        [FromForm] string? referenceName = null,
        [FromForm] string? referencePosition = null,
        [FromForm] string? referenceMail = null,
        [FromForm] string? referencePhone = null)
    {
        var employeeId = GetEmployeeId();
        if (employeeId == null) return Unauthorized(new { error = "EmployeeId claim not found" });

        var request = BuildUpdateRequest(variant, isVisible,
            introName, introStudyField, introDescription, introEmail, introPhone,
            skillNames,
            educationSchoolName, educationTime, educationDepartment, educationDescription,
            diplomaName, diplomaProvider, diplomaDate, diplomaLink,
            experienceJobName, experienceAddress, experienceStartDate, experienceEndDate, experienceDescription,
            projectName, projectDescription, projectRole, projectTechnology, projectLinksJson,
            awardName, awardDate, awardOrganization, awardDescription,
            activitiesName, activitiesDate, activitiesDescription,
            otherInfoDetail,
            referenceName, referencePosition, referenceMail, referencePhone);

        try
        {
            var block = await _blockService.UpdateBlockAsync(blockId, portfolioId, employeeId.Value, request, avatarFile, projectImageFile);
            return Ok(block);
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating block {BlockId}", blockId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    // ─── DELETE Block ─────────────────────────────────────────────────────────

    /// <summary>Delete a block and its data (cascade)</summary>
    [HttpDelete("{blockId:int}")]
    public async Task<IActionResult> DeleteBlock(int portfolioId, int blockId)
    {
        var employeeId = GetEmployeeId();
        if (employeeId == null) return Unauthorized(new { error = "EmployeeId claim not found" });

        try
        {
            await _blockService.DeleteBlockAsync(blockId, portfolioId, employeeId.Value);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting block {BlockId}", blockId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    // ─── REORDER Blocks ───────────────────────────────────────────────────────

    /// <summary>Reorder blocks within a portfolio</summary>
    [HttpPut("reorder")]
    public async Task<IActionResult> Reorder(int portfolioId, [FromBody] ReorderBlocksRequest request)
    {
        var employeeId = GetEmployeeId();
        if (employeeId == null) return Unauthorized(new { error = "EmployeeId claim not found" });

        try
        {
            await _blockService.ReorderBlocksAsync(portfolioId, employeeId.Value, request);
            return NoContent();
        }
        catch (KeyNotFoundException ex) { return NotFound(new { error = ex.Message }); }
        catch (UnauthorizedAccessException) { return Forbid(); }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reordering blocks in portfolio {PortfolioId}", portfolioId);
            return StatusCode(500, new { error = "Internal server error" });
        }
    }

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private int? GetEmployeeId()
    {
        var claim = User.FindFirst("employeeId")?.Value ?? User.FindFirst("EmployeeId")?.Value;
        if (!string.IsNullOrEmpty(claim) && int.TryParse(claim, out var id)) return id;
        var sub = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        if (!string.IsNullOrEmpty(sub) && int.TryParse(sub, out var sid)) return sid;
        return null;
    }

    private static AddBlockRequest BuildAddRequest(
        string blockTypeCode, string variant, int? displayOrder,
        string? introName, string? introStudyField, string? introDesc, string? introEmail, string? introPhone,
        string? skillNames,
        string? eduSchool, string? eduTime, string? eduDept, string? eduDesc,
        string? dipName, string? dipProvider, string? dipDate, string? dipLink,
        string? expJob, string? expAddr, string? expStart, string? expEnd, string? expDesc,
        string? projName, string? projDesc, string? projRole, string? projTech, string? projLinksJson,
        string? awardName, string? awardDate, string? awardOrg, string? awardDesc,
        string? actName, string? actDate, string? actDesc,
        string? otherDetail,
        string? refName, string? refPos, string? refMail, string? refPhone)
    {
        var req = new AddBlockRequest
        {
            BlockTypeCode = blockTypeCode,
            Variant = variant,
            DisplayOrder = displayOrder
        };

        switch (blockTypeCode.ToUpper())
        {
            case "INTRO":
                req.IntroData = new IntroDataRequest
                {
                    Name = introName, StudyField = introStudyField, Description = introDesc,
                    Email = introEmail, Phone = introPhone
                };
                break;
            case "SKILL":
                if (!string.IsNullOrEmpty(skillNames))
                    req.SkillData = skillNames.Split(',', StringSplitOptions.RemoveEmptyEntries)
                        .Select(n => new SkillDataRequest { Name = n.Trim() }).ToList();
                break;
            case "EDUCATION":
                req.EducationData = new EducationDataRequest
                {
                    SchoolName = eduSchool, Time = eduTime, Department = eduDept, Description = eduDesc
                };
                break;
            case "DIPLOMA":
                req.DiplomaData = new DiplomaDataRequest
                {
                    Name = dipName, Provider = dipProvider,
                    Date = DateOnly.TryParse(dipDate, out var dd) ? dd : null,
                    Link = dipLink
                };
                break;
            case "EXPERIMENT":
                req.ExperienceData = new ExperienceDataRequest
                {
                    JobName = expJob, Address = expAddr, StartDate = expStart, EndDate = expEnd, Description = expDesc
                };
                break;
            case "PROJECT":
                var links = new List<ProjectLinkRequest>();
                if (!string.IsNullOrEmpty(projLinksJson))
                    try { links = System.Text.Json.JsonSerializer.Deserialize<List<ProjectLinkRequest>>(projLinksJson) ?? links; }
                    catch { /* ignore parse error */ }
                req.ProjectData = new ProjectDataRequest
                {
                    Name = projName, Description = projDesc, Role = projRole, Technology = projTech, Links = links
                };
                break;
            case "AWARD":
                req.AwardData = new AwardDataRequest
                {
                    Name = awardName, Date = DateOnly.TryParse(awardDate, out var ad) ? ad : null,
                    Organization = awardOrg, Description = awardDesc
                };
                break;
            case "ACTIVITIES":
                req.ActivitiesData = new ActivitiesDataRequest
                {
                    Name = actName, Date = DateOnly.TryParse(actDate, out var aid) ? aid : null, Description = actDesc
                };
                break;
            case "OTHERINFO":
                req.OtherInfoData = new OtherInfoDataRequest { Detail = otherDetail };
                break;
            case "REFERENCE":
                req.ReferenceData = new ReferenceDataRequest
                {
                    Name = refName, Position = refPos, Mail = refMail, Phone = refPhone
                };
                break;
        }
        return req;
    }

    private static UpdateBlockRequest BuildUpdateRequest(
        string variant, bool isVisible,
        string? introName, string? introStudyField, string? introDesc, string? introEmail, string? introPhone,
        string? skillNames,
        string? eduSchool, string? eduTime, string? eduDept, string? eduDesc,
        string? dipName, string? dipProvider, string? dipDate, string? dipLink,
        string? expJob, string? expAddr, string? expStart, string? expEnd, string? expDesc,
        string? projName, string? projDesc, string? projRole, string? projTech, string? projLinksJson,
        string? awardName, string? awardDate, string? awardOrg, string? awardDesc,
        string? actName, string? actDate, string? actDesc,
        string? otherDetail,
        string? refName, string? refPos, string? refMail, string? refPhone)
    {
        return new UpdateBlockRequest
        {
            Variant = variant,
            IsVisible = isVisible,
            IntroData = introName != null || introStudyField != null ? new IntroDataRequest
            {
                Name = introName, StudyField = introStudyField, Description = introDesc,
                Email = introEmail, Phone = introPhone
            } : null,
            SkillData = !string.IsNullOrEmpty(skillNames)
                ? skillNames.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(n => new SkillDataRequest { Name = n.Trim() }).ToList()
                : null,
            EducationData = eduSchool != null ? new EducationDataRequest
            {
                SchoolName = eduSchool, Time = eduTime, Department = eduDept, Description = eduDesc
            } : null,
            DiplomaData = dipName != null ? new DiplomaDataRequest
            {
                Name = dipName, Provider = dipProvider,
                Date = DateOnly.TryParse(dipDate, out var dd) ? dd : null, Link = dipLink
            } : null,
            ExperienceData = expJob != null ? new ExperienceDataRequest
            {
                JobName = expJob, Address = expAddr, StartDate = expStart, EndDate = expEnd, Description = expDesc
            } : null,
            ProjectData = projName != null ? new ProjectDataRequest
            {
                Name = projName, Description = projDesc, Role = projRole, Technology = projTech,
                Links = !string.IsNullOrEmpty(projLinksJson)
                    ? (System.Text.Json.JsonSerializer.Deserialize<List<ProjectLinkRequest>>(projLinksJson) ?? new())
                    : new()
            } : null,
            AwardData = awardName != null ? new AwardDataRequest
            {
                Name = awardName, Date = DateOnly.TryParse(awardDate, out var ad) ? ad : null,
                Organization = awardOrg, Description = awardDesc
            } : null,
            ActivitiesData = actName != null ? new ActivitiesDataRequest
            {
                Name = actName, Date = DateOnly.TryParse(actDate, out var aid) ? aid : null, Description = actDesc
            } : null,
            OtherInfoData = otherDetail != null ? new OtherInfoDataRequest { Detail = otherDetail } : null,
            ReferenceData = refName != null ? new ReferenceDataRequest
            {
                Name = refName, Position = refPos, Mail = refMail, Phone = refPhone
            } : null
        };
    }
}

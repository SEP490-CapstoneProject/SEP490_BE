using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Portfolio.Application.DTOs;
using Portfolio.Application.Interfaces;
using Portfolio.Domain.Entities;

namespace Portfolio.Application.Services;

public class BlockService : IBlockService
{
    private readonly IBlockRepository _blockRepo;
    private readonly IPortfolioRepository _portfolioRepo;
    private readonly IMediaServiceClient _media;
    private readonly IMapper _mapper;
    private readonly ILogger<BlockService> _logger;

    public BlockService(
        IBlockRepository blockRepo,
        IPortfolioRepository portfolioRepo,
        IMediaServiceClient media,
        IMapper mapper,
        ILogger<BlockService> logger)
    {
        _blockRepo = blockRepo;
        _portfolioRepo = portfolioRepo;
        _media = media;
        _mapper = mapper;
        _logger = logger;
    }

    // ─── Add Block ────────────────────────────────────────────────────────────

    public async Task<BlockDto> AddBlockAsync(int portfolioId, int employeeId, AddBlockRequest request,
        IFormFile? avatarFile, IFormFile? projectImageFile)
    {
        var portfolio = await _portfolioRepo.GetByIdAsync(portfolioId)
            ?? throw new KeyNotFoundException($"Portfolio {portfolioId} not found");

        if (portfolio.EmployeeId != employeeId)
            throw new UnauthorizedAccessException("You do not own this portfolio");

        // Resolve block type from context (passed via request.BlockTypeCode)
        // We need BlockTypeId — look it up from block repository context
        // Block type IDs are seeded: INTRO=1, SKILL=2, EDUCATION=3, DIPLOMA=4,
        //   EXPERIMENT=5, PROJECT=6, AWARD=7, ACTIVITIES=8, OTHERINFO=9, REFERENCE=10
        var blockTypeInfo = GetBlockTypeInfo(request.BlockTypeCode);

        // Enforce IsMultiple = false (INTRO only 1 per portfolio)
        if (!blockTypeInfo.IsMultiple)
        {
            var count = await _blockRepo.CountByTypeAsync(portfolioId, blockTypeInfo.Id);
            if (count >= 1)
                throw new InvalidOperationException($"Block type '{request.BlockTypeCode}' allows only one per portfolio");
        }

        // Auto-assign order if not provided
        var order = request.DisplayOrder ?? (await _blockRepo.GetMaxOrderAsync(portfolioId) + 1);

        var block = new PortfolioBlock
        {
            PortfolioId = portfolioId,
            BlockTypeId = blockTypeInfo.Id,
            Variant = request.Variant,
            DisplayOrder = order,
            IsVisible = true
        };

        var created = await _blockRepo.CreateAsync(block);

        // Insert block-type-specific data
        await InsertBlockDataAsync(created.Id, request.BlockTypeCode, request, avatarFile, projectImageFile);

        // Reload with all data included
        var full = await _blockRepo.GetByIdWithDataAsync(created.Id);
        return MapBlockToDto(full!);
    }

    // ─── Update Block ─────────────────────────────────────────────────────────

    public async Task<BlockDto> UpdateBlockAsync(int blockId, int portfolioId, int employeeId,
        UpdateBlockRequest request, IFormFile? avatarFile, IFormFile? projectImageFile)
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
        await _blockRepo.UpdateAsync(block);

        await UpdateBlockDataAsync(block, request, avatarFile, projectImageFile);

        var full = await _blockRepo.GetByIdWithDataAsync(blockId);
        return MapBlockToDto(full!);
    }

    // ─── Delete Block ─────────────────────────────────────────────────────────

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

    // ─── Reorder Blocks ───────────────────────────────────────────────────────

    public async Task ReorderBlocksAsync(int portfolioId, int employeeId, ReorderBlocksRequest request)
    {
        var portfolio = await _portfolioRepo.GetByIdAsync(portfolioId)
            ?? throw new KeyNotFoundException($"Portfolio {portfolioId} not found");

        if (portfolio.EmployeeId != employeeId)
            throw new UnauthorizedAccessException("You do not own this portfolio");

        var reorders = request.Items.Select(i => (i.BlockId, i.DisplayOrder)).ToList();
        await _blockRepo.ReorderAsync(reorders);
    }

    // ─── Insert Data (Create) ─────────────────────────────────────────────────

    private async Task InsertBlockDataAsync(int blockId, string blockTypeCode, AddBlockRequest request,
        IFormFile? avatarFile, IFormFile? projectImageFile)
    {
        switch (blockTypeCode.ToUpper())
        {
            case "INTRO":
                var intro = new Intro { PortfolioBlockId = blockId };
                if (request.IntroData != null)
                {
                    intro.Name = request.IntroData.Name;
                    intro.StudyField = request.IntroData.StudyField;
                    intro.Description = request.IntroData.Description;
                    intro.Email = request.IntroData.Email;
                    intro.Phone = request.IntroData.Phone;
                }
                if (avatarFile != null)
                {
                    var (success, url, _) = await _media.UploadImageAsync(avatarFile);
                    if (success) intro.Avatar = url;
                }
                await _blockRepo.AddIntroAsync(intro);
                break;

            case "SKILL":
                if (request.SkillData != null && request.SkillData.Count > 0)
                {
                    var skills = request.SkillData.Select(s => new Skill
                    {
                        PortfolioBlockId = blockId,
                        Name = s.Name
                    });
                    await _blockRepo.AddSkillsAsync(skills);
                }
                break;

            case "EDUCATION":
                if (request.EducationData != null)
                    await _blockRepo.AddEducationAsync(new Education
                    {
                        PortfolioBlockId = blockId,
                        SchoolName = request.EducationData.SchoolName,
                        Time = request.EducationData.Time,
                        Department = request.EducationData.Department,
                        Description = request.EducationData.Description
                    });
                break;

            case "DIPLOMA":
                if (request.DiplomaData != null)
                    await _blockRepo.AddDiplomaAsync(new Diploma
                    {
                        PortfolioBlockId = blockId,
                        Name = request.DiplomaData.Name,
                        Provider = request.DiplomaData.Provider,
                        Date = request.DiplomaData.Date,
                        Link = request.DiplomaData.Link
                    });
                break;

            case "EXPERIMENT":
                if (request.ExperienceData != null)
                    await _blockRepo.AddExperienceAsync(new Experience
                    {
                        PortfolioBlockId = blockId,
                        JobName = request.ExperienceData.JobName,
                        Address = request.ExperienceData.Address,
                        StartDate = request.ExperienceData.StartDate,
                        EndDate = request.ExperienceData.EndDate,
                        Description = request.ExperienceData.Description
                    });
                break;

            case "PROJECT":
                if (request.ProjectData != null)
                {
                    var imageUrl = string.Empty;
                    if (projectImageFile != null)
                    {
                        var (ok, url, _) = await _media.UploadImageAsync(projectImageFile);
                        if (ok) imageUrl = url;
                    }
                    var project = new Project
                    {
                        PortfolioBlockId = blockId,
                        Image = imageUrl,
                        Name = request.ProjectData.Name,
                        Description = request.ProjectData.Description,
                        Role = request.ProjectData.Role,
                        Technology = request.ProjectData.Technology,
                        Links = request.ProjectData.Links.Select(l => new ProjectLink
                        {
                            Type = l.Type,
                            Link = l.Link
                        }).ToList()
                    };
                    await _blockRepo.AddProjectAsync(project);
                }
                break;

            case "AWARD":
                if (request.AwardData != null)
                    await _blockRepo.AddAwardAsync(new Award
                    {
                        PortfolioBlockId = blockId,
                        Name = request.AwardData.Name,
                        Date = request.AwardData.Date,
                        Organization = request.AwardData.Organization,
                        Description = request.AwardData.Description
                    });
                break;

            case "ACTIVITIES":
                if (request.ActivitiesData != null)
                    await _blockRepo.AddActivitiesAsync(new Activities
                    {
                        PortfolioBlockId = blockId,
                        Name = request.ActivitiesData.Name,
                        Date = request.ActivitiesData.Date,
                        Description = request.ActivitiesData.Description
                    });
                break;

            case "OTHERINFO":
                if (request.OtherInfoData != null)
                    await _blockRepo.AddOtherInfoAsync(new OtherInfo
                    {
                        PortfolioBlockId = blockId,
                        Detail = request.OtherInfoData.Detail
                    });
                break;

            case "REFERENCE":
                if (request.ReferenceData != null)
                    await _blockRepo.AddReferenceAsync(new Reference
                    {
                        PortfolioBlockId = blockId,
                        Name = request.ReferenceData.Name,
                        Position = request.ReferenceData.Position,
                        Mail = request.ReferenceData.Mail,
                        Phone = request.ReferenceData.Phone
                    });
                break;

            default:
                _logger.LogWarning("Unknown block type code: {Code}", blockTypeCode);
                break;
        }
    }

    // ─── Update Data ──────────────────────────────────────────────────────────

    private async Task UpdateBlockDataAsync(PortfolioBlock block, UpdateBlockRequest request,
        IFormFile? avatarFile, IFormFile? projectImageFile)
    {
        var code = block.BlockType.Code.ToUpper();
        switch (code)
        {
            case "INTRO":
                if (request.IntroData != null && block.Intro != null)
                {
                    block.Intro.Name = request.IntroData.Name;
                    block.Intro.StudyField = request.IntroData.StudyField;
                    block.Intro.Description = request.IntroData.Description;
                    block.Intro.Email = request.IntroData.Email;
                    block.Intro.Phone = request.IntroData.Phone;
                    if (avatarFile != null)
                    {
                        var (ok, url, _) = await _media.UploadImageAsync(avatarFile);
                        if (ok) block.Intro.Avatar = url;
                    }
                    await _blockRepo.UpdateIntroAsync(block.Intro);
                }
                break;

            case "SKILL":
                if (request.SkillData != null)
                {
                    await _blockRepo.RemoveSkillsAsync(block.Id);
                    var newSkills = request.SkillData.Select(s => new Skill
                    {
                        PortfolioBlockId = block.Id,
                        Name = s.Name
                    });
                    await _blockRepo.AddSkillsAsync(newSkills);
                }
                break;

            case "EDUCATION":
                if (request.EducationData != null && block.Educations.FirstOrDefault() is { } edu)
                {
                    edu.SchoolName = request.EducationData.SchoolName;
                    edu.Time = request.EducationData.Time;
                    edu.Department = request.EducationData.Department;
                    edu.Description = request.EducationData.Description;
                    await _blockRepo.UpdateEducationAsync(edu);
                }
                break;

            case "DIPLOMA":
                if (request.DiplomaData != null && block.Diplomas.FirstOrDefault() is { } dip)
                {
                    dip.Name = request.DiplomaData.Name;
                    dip.Provider = request.DiplomaData.Provider;
                    dip.Date = request.DiplomaData.Date;
                    dip.Link = request.DiplomaData.Link;
                    await _blockRepo.UpdateDiplomaAsync(dip);
                }
                break;

            case "EXPERIMENT":
                if (request.ExperienceData != null && block.Experiences.FirstOrDefault() is { } exp)
                {
                    exp.JobName = request.ExperienceData.JobName;
                    exp.Address = request.ExperienceData.Address;
                    exp.StartDate = request.ExperienceData.StartDate;
                    exp.EndDate = request.ExperienceData.EndDate;
                    exp.Description = request.ExperienceData.Description;
                    await _blockRepo.UpdateExperienceAsync(exp);
                }
                break;

            case "PROJECT":
                if (request.ProjectData != null && block.Projects.FirstOrDefault() is { } proj)
                {
                    if (projectImageFile != null)
                    {
                        var (ok, url, _) = await _media.UploadImageAsync(projectImageFile);
                        if (ok) proj.Image = url;
                    }
                    proj.Name = request.ProjectData.Name;
                    proj.Description = request.ProjectData.Description;
                    proj.Role = request.ProjectData.Role;
                    proj.Technology = request.ProjectData.Technology;
                    proj.Links = request.ProjectData.Links.Select(l => new ProjectLink
                    {
                        ProjectId = proj.Id,
                        Type = l.Type,
                        Link = l.Link
                    }).ToList();
                    await _blockRepo.UpdateProjectAsync(proj);
                }
                break;

            case "AWARD":
                if (request.AwardData != null && block.Awards.FirstOrDefault() is { } award)
                {
                    award.Name = request.AwardData.Name;
                    award.Date = request.AwardData.Date;
                    award.Organization = request.AwardData.Organization;
                    award.Description = request.AwardData.Description;
                    await _blockRepo.UpdateAwardAsync(award);
                }
                break;

            case "ACTIVITIES":
                if (request.ActivitiesData != null && block.Activities.FirstOrDefault() is { } act)
                {
                    act.Name = request.ActivitiesData.Name;
                    act.Date = request.ActivitiesData.Date;
                    act.Description = request.ActivitiesData.Description;
                    await _blockRepo.UpdateActivitiesAsync(act);
                }
                break;

            case "OTHERINFO":
                if (request.OtherInfoData != null && block.OtherInfos.FirstOrDefault() is { } info)
                {
                    info.Detail = request.OtherInfoData.Detail;
                    await _blockRepo.UpdateOtherInfoAsync(info);
                }
                break;

            case "REFERENCE":
                if (request.ReferenceData != null && block.References.FirstOrDefault() is { } reference)
                {
                    reference.Name = request.ReferenceData.Name;
                    reference.Position = request.ReferenceData.Position;
                    reference.Mail = request.ReferenceData.Mail;
                    reference.Phone = request.ReferenceData.Phone;
                    await _blockRepo.UpdateReferenceAsync(reference);
                }
                break;
        }
    }

    // ─── Mapping ──────────────────────────────────────────────────────────────

    public BlockDto MapBlockToDto(PortfolioBlock block)
    {
        var code = block.BlockType?.Code?.ToUpper() ?? string.Empty;
        object data = code switch
        {
            "INTRO" => _mapper.Map<IntroDataDto>(block.Intro),
            "SKILL" => _mapper.Map<List<SkillDataDto>>(block.Skills),
            "EDUCATION" => _mapper.Map<List<EducationDataDto>>(block.Educations),
            "DIPLOMA" => _mapper.Map<List<DiplomaDataDto>>(block.Diplomas),
            "EXPERIMENT" => _mapper.Map<List<ExperienceDataDto>>(block.Experiences),
            "PROJECT" => _mapper.Map<List<ProjectDataDto>>(block.Projects),
            "AWARD" => _mapper.Map<List<AwardDataDto>>(block.Awards),
            "ACTIVITIES" => _mapper.Map<List<ActivitiesDataDto>>(block.Activities),
            "OTHERINFO" => _mapper.Map<List<OtherInfoDataDto>>(block.OtherInfos),
            "REFERENCE" => _mapper.Map<List<ReferenceDataDto>>(block.References),
            _ => new object()
        };

        return new BlockDto
        {
            Id = block.Id,
            Type = code,
            Variant = block.Variant,
            Order = block.DisplayOrder,
            Data = data
        };
    }

    // ─── Helper ───────────────────────────────────────────────────────────────

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

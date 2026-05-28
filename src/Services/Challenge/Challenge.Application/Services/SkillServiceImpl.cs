using System.Text;
using Challenge.Application.DTOs;
using Challenge.Application.Interfaces;
using Challenge.Domain.Entities;
using Challenge.Domain.Repositories;

namespace Challenge.Application.Services;

public class SkillService : ISkillService
{
    private readonly ISkillRepository _skillRepository;
    private readonly IUserSkillRepository _userSkillRepository;
    private readonly ILogger<SkillService> _logger;

    public SkillService(
        ISkillRepository skillRepository,
        IUserSkillRepository userSkillRepository,
        ILogger<SkillService> logger)
    {
        _skillRepository = skillRepository;
        _userSkillRepository = userSkillRepository;
        _logger = logger;
    }

    public async Task<SkillDto> CreateSkillAsync(CreateSkillDto request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var skill = new Skill
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Slug = NormalizeSlug(request.Name),
            Description = string.Empty,
            CategoryId = request.CategoryId == Guid.Empty ? null : request.CategoryId,
            IsApproved = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _skillRepository.AddAsync(skill);
        _logger.LogInformation("Created skill {SkillId} ({Slug})", skill.Id, skill.Slug);
        return Map(skill);
    }

    public async Task<SkillDto?> GetSkillByIdAsync(int id)
    {
        var skills = await _skillRepository.GetAllAsync();
        var skill = skills.ElementAtOrDefault(id <= 0 ? -1 : id - 1);
        return skill is null ? null : Map(skill);
    }

    public async Task<List<SkillDto>> ListSkillsAsync()
    {
        var skills = await _skillRepository.GetAllAsync();
        return skills.Select(Map).ToList();
    }

    public async Task<SkillDto> UpdateSkillAsync(int id, UpdateSkillDto request)
    {
        var skills = await _skillRepository.GetAllAsync();
        var skill = skills.ElementAtOrDefault(id <= 0 ? -1 : id - 1);
        if (skill is null)
        {
            throw new KeyNotFoundException($"Skill {id} not found");
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            skill.Name = request.Name;
            skill.Slug = NormalizeSlug(request.Name);
        }

        if (request.Description is not null)
        {
            skill.Description = request.Description;
        }

        skill.UpdatedAt = DateTime.UtcNow;
        await _skillRepository.UpdateAsync(skill);
        return Map(skill);
    }

    public async Task DeleteSkillAsync(int id)
    {
        var skills = await _skillRepository.GetAllAsync();
        var skill = skills.ElementAtOrDefault(id <= 0 ? -1 : id - 1);
        if (skill is null)
        {
            return;
        }

        await _skillRepository.DeleteAsync(skill.Id);
    }

    public async Task<List<SkillDto>> SearchSkillsAsync(string query)
    {
        var skills = await _skillRepository.FuzzySearchAsync(query);
        return skills.Select(Map).ToList();
    }

    public async Task<SkillDto?> GetSkillBySlugAsync(string slug)
    {
        var skill = await _skillRepository.GetBySlugAsync(slug);
        return skill is null ? null : Map(skill);
    }

    public async Task<UserSkillDto> GetUserSkillAsync(int userId, int skillId)
    {
        return new UserSkillDto { UserId = userId, SkillId = Guid.Empty };
    }

    public async Task<List<UserSkillDto>> GetUserSkillsAsync(int userId)
    {
        var userSkills = await _userSkillRepository.GetByUserAsync(userId);
        return userSkills.Select(Map).ToList();
    }

    public async Task<List<UserSkillDto>> GetVerifiedSkillsAsync(int userId)
    {
        var userSkills = await _userSkillRepository.GetVerifiedByUserAsync(userId);
        return userSkills.Select(Map).ToList();
    }

    public async Task<List<UserSkillDto>> GetSkillsByVerificationLevelAsync(int userId, string level)
    {
        var userSkills = await _userSkillRepository.GetByUserAsync(userId);
        return userSkills
            .Where(s => string.Equals(s.VerificationLevel.ToString(), level, StringComparison.OrdinalIgnoreCase))
            .Select(Map)
            .ToList();
    }

    private static SkillDto Map(Skill skill) => new()
    {
        Id = skill.Id,
        Name = skill.Name,
        Slug = skill.Slug,
        IsSystem = false,
        IsApproved = skill.IsApproved
    };

    private static UserSkillDto Map(UserSkill userSkill) => new()
    {
        UserId = userSkill.UserId,
        SkillId = userSkill.SkillId,
        TotalPoints = userSkill.TotalPoints,
        MasteryScore = userSkill.MasteryScore,
        VerificationLevel = userSkill.VerificationLevel.ToString(),
        ChallengeCount = userSkill.VerifiedChallengeCount,
        LastVerifiedAt = userSkill.LastVerifiedAt
    };

    private static string NormalizeSlug(string value)
        => string.Join(
            "-",
            value.Trim().ToLowerInvariant().Split(new[] { ' ', '\t', '\r', '\n', '/', '\\', '+', '.', ',', ':', ';', '(', ')' }, StringSplitOptions.RemoveEmptyEntries));
}

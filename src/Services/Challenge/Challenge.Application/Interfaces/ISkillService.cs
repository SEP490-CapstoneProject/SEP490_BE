using Challenge.Application.DTOs;

namespace Challenge.Application.Interfaces;

/// <summary>
/// Service for managing skills, skill normalization, and verification levels
/// </summary>
public interface ISkillService
{
    // CRUD
    Task<SkillDto> CreateSkillAsync(CreateSkillDto request);
    Task<SkillDto?> GetSkillByIdAsync(int id);
    Task<List<SkillDto>> ListSkillsAsync();
    Task<SkillDto> UpdateSkillAsync(int id, UpdateSkillDto request);
    Task DeleteSkillAsync(int id);

    // Search and discovery
    Task<List<SkillDto>> SearchSkillsAsync(string query);
    Task<SkillDto?> GetSkillBySlugAsync(string slug);

    // User skills
    Task<UserSkillDto> GetUserSkillAsync(int userId, int skillId);
    Task<List<UserSkillDto>> GetUserSkillsAsync(int userId);
    Task<List<UserSkillDto>> GetVerifiedSkillsAsync(int userId);

    // Skill verification levels
    Task<List<UserSkillDto>> GetSkillsByVerificationLevelAsync(int userId, string level);
}

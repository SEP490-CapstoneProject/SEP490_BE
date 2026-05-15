using Challenge.Application.DTOs;
using Challenge.Application.Interfaces;

namespace Challenge.Application.Services;

public class SkillService : ISkillService
{
    private readonly ILogger<SkillService> _logger;

    public SkillService(
        ISkillRepository skillRepository,
        IUserSkillRepository userSkillRepository,
        ILogger<SkillService> logger)
    {
        _logger = logger;
    }

    public Task<SkillDto> CreateSkillAsync(CreateSkillDto request)
        => Task.FromResult(new SkillDto { Id = Guid.NewGuid(), Name = request.Name, Slug = request.Name.ToLowerInvariant().Replace(" ", "-"), IsSystem = false, IsApproved = false });

    public Task<SkillDto?> GetSkillByIdAsync(int id) => Task.FromResult<SkillDto?>(null);

    public Task<List<SkillDto>> ListSkillsAsync() => Task.FromResult(new List<SkillDto>());

    public Task<SkillDto> UpdateSkillAsync(int id, UpdateSkillDto request)
        => Task.FromResult(new SkillDto { Id = Guid.NewGuid(), Name = request.Name ?? string.Empty });

    public Task DeleteSkillAsync(int id) => Task.CompletedTask;

    public Task<List<SkillDto>> SearchSkillsAsync(string query) => Task.FromResult(new List<SkillDto>());

    public Task<SkillDto?> GetSkillBySlugAsync(string slug) => Task.FromResult<SkillDto?>(null);

    public Task<UserSkillDto> GetUserSkillAsync(int userId, int skillId)
        => Task.FromResult(new UserSkillDto { UserId = userId, SkillId = skillId });

    public Task<List<UserSkillDto>> GetUserSkillsAsync(int userId) => Task.FromResult(new List<UserSkillDto>());

    public Task<List<UserSkillDto>> GetVerifiedSkillsAsync(int userId) => Task.FromResult(new List<UserSkillDto>());

    public Task<List<UserSkillDto>> GetSkillsByVerificationLevelAsync(int userId, string level) => Task.FromResult(new List<UserSkillDto>());
}

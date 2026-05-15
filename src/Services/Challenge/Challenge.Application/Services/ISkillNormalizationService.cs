using Challenge.Domain.Entities;

namespace Challenge.Application.Services;

public interface ISkillNormalizationService
{
    Task<Dictionary<Guid, decimal>> NormalizeSkillsAsync(Dictionary<string, decimal> detectedSkills, CancellationToken cancellationToken = default);
    Task<Guid> ResolveSkillAsync(string skillName, CancellationToken cancellationToken = default);
}

public class SkillNormalizationService : ISkillNormalizationService
{
    public SkillNormalizationService(ISkillRepository skillRepository, ILogger<SkillNormalizationService> logger)
    {
    }

    public Task<Dictionary<Guid, decimal>> NormalizeSkillsAsync(Dictionary<string, decimal> detectedSkills, CancellationToken cancellationToken = default)
        => Task.FromResult(detectedSkills.ToDictionary(kvp => Guid.NewGuid(), kvp => kvp.Value));

    public Task<Guid> ResolveSkillAsync(string skillName, CancellationToken cancellationToken = default)
        => Task.FromResult(Guid.NewGuid());
}

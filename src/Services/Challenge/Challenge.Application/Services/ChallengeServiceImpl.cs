using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Challenge.Application.Clients;
using Challenge.Application.DTOs;
using Challenge.Application.Interfaces;
using Challenge.Domain.Entities;
using Challenge.Domain.Enums;
using Challenge.Domain.Repositories;
using ChallengeEntity = Challenge.Domain.Entities.Challenge;

namespace Challenge.Application.Services;

public class ChallengeService : IChallengeService
{
    private readonly IChallengeRepository _challengeRepository;
    private readonly IChallengeVersionRepository _versionRepository;
    private readonly ISkillRepository _skillRepository;
    private readonly IEvaluationCriteriaRepository _evaluationCriteriaRepository;
    private readonly IChallengeCriteriaRepository _challengeCriteriaRepository;
    private readonly ICriteriaSkillMappingRepository _criteriaSkillMappingRepository;
    private readonly IGeminiAIService _geminiAIService;
    private readonly IEventPublisher _eventPublisher;
    private readonly IActorResolverClient _actorResolverClient;
    private readonly ILogger<ChallengeService> _logger;

    public ChallengeService(
        IChallengeRepository repository,
        IChallengeVersionRepository versionRepository,
        ISkillRepository skillRepository,
        IEvaluationCriteriaRepository evaluationCriteriaRepository,
        IChallengeCriteriaRepository challengeCriteriaRepository,
        ICriteriaSkillMappingRepository criteriaSkillMappingRepository,
        IGeminiAIService geminiAIService,
        IEventPublisher eventPublisher,
        IActorResolverClient actorResolverClient,
        ILogger<ChallengeService> logger)
    {
        _challengeRepository = repository;
        _versionRepository = versionRepository;
        _skillRepository = skillRepository;
        _evaluationCriteriaRepository = evaluationCriteriaRepository;
        _challengeCriteriaRepository = challengeCriteriaRepository;
        _criteriaSkillMappingRepository = criteriaSkillMappingRepository;
        _geminiAIService = geminiAIService;
        _eventPublisher = eventPublisher;
        _actorResolverClient = actorResolverClient;
        _logger = logger;
    }

    public async Task<ChallengeDto> CreateChallengeAsync(CreateChallengeDto request, int userId)
    {
        ArgumentNullException.ThrowIfNull(request);

        var now = DateTime.UtcNow;
        var challenge = new ChallengeEntity
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            ExpectedSolution = request.ExpectedSolution,
            DifficultyScore = 0,
            DifficultyLabel = "Medium",
            Status = ChallengeStatus.Draft,
            CurrentVersionId = null,
            CreatedById = userId,
            ReviewedById = null,
            RejectionReason = string.Empty,
            Deadline = request.Deadline,
            PublishedAt = null,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _challengeRepository.AddAsync(challenge);
        var persisted = await _challengeRepository.ExistsAsync(challenge.Id);
        _logger.LogInformation("Challenge {ChallengeId} persisted after create: {Persisted}", challenge.Id, persisted);
        return MapToDto(challenge);
    }

    public async Task<ChallengeDto?> GetChallengeByIdAsync(Guid id, int? currentUserId)
    {
        var challenge = await _challengeRepository.GetByIdAsync(id);
        if (challenge is null)
        {
            return null;
        }

        // Validate ownership: only creator can view own challenges in Draft/Rejected status
        // Published challenges are visible to all
        if (challenge.Status != ChallengeStatus.Published && currentUserId.HasValue)
        {
            if (challenge.CreatedById != currentUserId.Value)
            {
                throw new UnauthorizedAccessException("You do not have permission to view this challenge.");
            }
        }

        return MapToDto(challenge);
    }

    public async Task<List<ChallengeDto>> ListChallengesAsync(int pageSize = 20, int? cursor = null, int? currentUserId = null)
    {
        var challenges = await _challengeRepository.GetAllAsync();
        
        // Filter: only show published challenges to other users, or all challenges to the creator
        var filtered = challenges.Where(c =>
        {
            if (currentUserId.HasValue)
            {
                // Show: published challenges to everyone, or own challenges to creator
                return c.Status == ChallengeStatus.Published || c.CreatedById == currentUserId.Value;
            }
            // Unauthenticated users see only published challenges
            return c.Status == ChallengeStatus.Published;
        });

        return filtered
            .OrderByDescending(c => c.CreatedAt)
            .Take(pageSize)
            .Select(MapToDto)
            .ToList();
    }

    public async Task<ChallengeDto> UpdateChallengeAsync(Guid id, UpdateChallengeDto request, int userId)
    {
        ArgumentNullException.ThrowIfNull(request);

        var challenge = await _challengeRepository.GetByIdAsync(id);
        if (challenge is null)
        {
            throw new KeyNotFoundException($"Challenge {id} not found");
        }

        EnsureOwner(challenge, userId);

        challenge.Title = request.Title ?? challenge.Title;
        challenge.Description = request.Description ?? challenge.Description;
        challenge.ExpectedSolution = request.ExpectedSolution ?? challenge.ExpectedSolution;
        challenge.Deadline = request.Deadline == default ? challenge.Deadline : request.Deadline;
        challenge.UpdatedAt = DateTime.UtcNow;

        await _challengeRepository.UpdateAsync(challenge);
        return MapToDto(challenge);
    }

    public async Task DeleteChallengeAsync(Guid id, int userId)
    {
        var challenge = await _challengeRepository.GetByIdAsync(id);
        if (challenge is null)
        {
            throw new KeyNotFoundException($"Challenge {id} not found");
        }

        EnsureOwner(challenge, userId);
        await _challengeRepository.DeleteAsync(id);
    }

    public async Task<ChallengeDto> SubmitForReviewAsync(Guid id, int userId)
    {
        var challenge = await _challengeRepository.GetByIdAsync(id);
        if (challenge is null)
        {
            throw new KeyNotFoundException($"Challenge {id} not found");
        }

        EnsureOwner(challenge, userId);

        var analysis = await _geminiAIService.AnalyzeChallengeAsync(
            challenge.Title,
            challenge.Description,
            challenge.ExpectedSolution);

        await EnsureSkillsFromAnalysisAsync(challenge, analysis);

        var nextVersionNumber = await GetNextVersionNumberAsync(challenge.Id);
        var version = new ChallengeVersion
        {
            Id = Guid.NewGuid(),
            ChallengeId = challenge.Id,
            VersionNumber = nextVersionNumber,
            Title = challenge.Title,
            Description = challenge.Description,
            ExpectedSolution = challenge.ExpectedSolution,
            DifficultyScore = analysis.DifficultyScore,
            DifficultyLabel = analysis.DifficultyLabel,
            SkillWeightMapping = JsonSerializer.Serialize(analysis.SkillWeights),
            ModelName = "Gemini 1.5 Pro",
            PromptVersion = "v1.0",
            EvaluatedAt = DateTime.UtcNow,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _versionRepository.AddAsync(version);
        await PersistCriteriaModelAsync(version, analysis);
        _logger.LogInformation("Challenge {ChallengeId} analyzed by AI and version {VersionId} created", challenge.Id, version.Id);

        challenge.CurrentVersionId = version.Id;
        challenge.Status = ChallengeStatus.PendingReview;
        challenge.UpdatedAt = DateTime.UtcNow;

        await _challengeRepository.UpdateAsync(challenge);
        return MapToDto(challenge);
    }

    public async Task<ChallengeDto> ApproveChallengeAsync(Guid id, int adminId)
    {
        var challenge = await _challengeRepository.GetByIdAsync(id);
        if (challenge is null)
        {
            throw new KeyNotFoundException($"Challenge {id} not found");
        }

        challenge.Status = ChallengeStatus.Published;
        challenge.ReviewedById = adminId;
        challenge.PublishedAt = DateTime.UtcNow;
        challenge.RejectionReason = string.Empty;
        challenge.UpdatedAt = DateTime.UtcNow;

        await _challengeRepository.UpdateAsync(challenge);
        return MapToDto(challenge);
    }

    public async Task<ChallengeDto> RejectChallengeAsync(Guid id, string reason, int adminId)
    {
        var challenge = await _challengeRepository.GetByIdAsync(id);
        if (challenge is null)
        {
            throw new KeyNotFoundException($"Challenge {id} not found");
        }

        challenge.Status = ChallengeStatus.Rejected;
        challenge.ReviewedById = adminId;
        challenge.RejectionReason = reason;
        challenge.UpdatedAt = DateTime.UtcNow;

        await _challengeRepository.UpdateAsync(challenge);
        return MapToDto(challenge);
    }

    public async Task<(List<ChallengeDto> items, int totalCount)> GetChallengesPagedAsync(
        int skip,
        int take,
        string? status = null,
        int? userId = null)
    {
        IEnumerable<ChallengeEntity> challenges = await ResolveChallengesAsync(status);

        if (userId.HasValue)
        {
            challenges = challenges.Where(challenge => challenge.CreatedById == userId.Value);
        }

        var ordered = challenges
            .OrderByDescending(challenge => challenge.CreatedAt)
            .ToList();

        var totalCount = ordered.Count;
        _logger.LogInformation(
            "Challenge page query returned {Count} records (skip={Skip}, take={Take}, status={Status}, userId={UserId})",
            totalCount,
            skip,
            take,
            status ?? "<all>",
            userId?.ToString() ?? "<none>");
        var items = ordered
            .Skip(skip)
            .Take(take)
            .Select(MapToDto)
            .ToList();

        return (items, totalCount);
    }

    public async Task<(List<CreatorChallengeDto> items, int totalCount)> GetCreatorChallengesAsync(
        int userId,
        int skip,
        int take)
    {
        var allChallenges = await _challengeRepository.GetAllAsync();
        
        var creatorChallenges = allChallenges
            .Where(c => c.CreatedById == userId)
            .OrderByDescending(c => c.CreatedAt)
            .ToList();

        var totalCount = creatorChallenges.Count;
        var items = creatorChallenges
            .Skip(skip)
            .Take(take)
            .Select(c => new CreatorChallengeDto
            {
                Id = c.Id,
                Title = c.Title,
                Description = c.Description,
                Status = c.Status.ToString(),
                CurrentVersionId = c.CurrentVersionId,
                CreatedAt = c.CreatedAt,
                UpdatedAt = c.UpdatedAt,
                Deadline = c.Deadline
            })
            .ToList();

        return (items, totalCount);
    }

    public async Task<List<ChallengeVersionDto>> GetChallengeVersionsAsync(Guid challengeId, int creatorUserId)
    {
        var challenge = await _challengeRepository.GetByIdAsync(challengeId);
        if (challenge is null)
        {
            throw new KeyNotFoundException($"Challenge {challengeId} not found");
        }

        // Verify ownership
        EnsureOwner(challenge, creatorUserId);

        var versions = await _versionRepository.GetVersionsByChallengeAsync(challengeId);
        var versionDtos = new List<ChallengeVersionDto>();

        foreach (var version in versions)
        {
            var dto = new ChallengeVersionDto
            {
                Id = version.Id,
                ChallengeId = version.ChallengeId,
                VersionNumber = version.VersionNumber,
                Title = version.Title,
                Description = version.Description,
                ExpectedSolution = version.ExpectedSolution,
                DifficultyScore = version.DifficultyScore,
                DifficultyLabel = version.DifficultyLabel,
                SkillWeightMapping = version.SkillWeightMapping,
                ModelName = version.ModelName,
                PromptVersion = version.PromptVersion,
                EvaluatedAt = version.EvaluatedAt,
                IsActive = version.IsActive,
                CreatedAt = version.CreatedAt
            };

            // Load criteria for this version
            var criteria = await _challengeCriteriaRepository.GetByVersionAsync(version.Id);
            dto.Criteria = criteria
                .Select(c => new ChallengeVersionCriteriaDto
                {
                    Id = c.Id,
                    VersionId = c.ChallengeVersionId,
                    Name = c.Criteria?.Name ?? "Unknown",
                    Description = c.Criteria?.Description ?? "",
                    Weight = c.Weight,
                    MaxScore = 100, // Default max score
                    DisplayOrder = 0
                })
                .ToList();

            // Load skill mappings
            if (!string.IsNullOrEmpty(version.SkillWeightMapping))
            {
                try
                {
                    var skillWeights = JsonSerializer.Deserialize<Dictionary<string, decimal>>(version.SkillWeightMapping) ?? new();
                    dto.SkillMappings = skillWeights
                        .Select(sw => new VersionSkillMappingDto
                        {
                            Id = Guid.NewGuid(), // Placeholder
                            VersionId = version.Id,
                            SkillName = sw.Key,
                            Weight = sw.Value,
                            CriteriaIds = criteria
                                .Where(c => c.Criteria?.Name == sw.Key)
                                .Select(c => c.Id)
                                .ToList()
                        })
                        .ToList();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to parse skill weights for version {VersionId}", version.Id);
                }
            }

            versionDtos.Add(dto);
        }

        return versionDtos;
    }

    public async Task<ChallengeVersionDto> SetActiveVersionAsync(Guid challengeId, Guid versionId, int creatorUserId)
    {
        var challenge = await _challengeRepository.GetByIdAsync(challengeId);
        if (challenge is null)
        {
            throw new KeyNotFoundException($"Challenge {challengeId} not found");
        }

        // Verify ownership
        EnsureOwner(challenge, creatorUserId);

        var version = await _versionRepository.GetByIdAsync(versionId);
        if (version is null || version.ChallengeId != challengeId)
        {
            throw new KeyNotFoundException($"Version {versionId} not found for challenge {challengeId}");
        }

        // Update the challenge's active version
        challenge.CurrentVersionId = versionId;
        challenge.UpdatedAt = DateTime.UtcNow;
        await _challengeRepository.UpdateAsync(challenge);

        _logger.LogInformation("Set version {VersionId} as active for challenge {ChallengeId}", versionId, challengeId);

        // Return the full version details
        var criteria = await _challengeCriteriaRepository.GetByVersionAsync(version.Id);
        var dto = new ChallengeVersionDto
        {
            Id = version.Id,
            ChallengeId = version.ChallengeId,
            VersionNumber = version.VersionNumber,
            Title = version.Title,
            Description = version.Description,
            ExpectedSolution = version.ExpectedSolution,
            DifficultyScore = version.DifficultyScore,
            DifficultyLabel = version.DifficultyLabel,
            SkillWeightMapping = version.SkillWeightMapping,
            ModelName = version.ModelName,
            PromptVersion = version.PromptVersion,
            EvaluatedAt = version.EvaluatedAt,
            IsActive = true,
            CreatedAt = version.CreatedAt
        };

        dto.Criteria = criteria
            .Select(c => new ChallengeVersionCriteriaDto
            {
                Id = c.Id,
                VersionId = c.ChallengeVersionId,
                Name = c.Criteria?.Name ?? "Unknown",
                Description = c.Criteria?.Description ?? "",
                Weight = c.Weight,
                MaxScore = 100,
                DisplayOrder = 0
            })
            .ToList();

        return dto;
    }

    public async Task<CreatorChallengeDto> ApproveAndPublishAsync(Guid challengeId, int creatorUserId)
    {
        var challenge = await _challengeRepository.GetByIdAsync(challengeId);
        if (challenge is null)
        {
            throw new KeyNotFoundException($"Challenge {challengeId} not found");
        }

        // Verify ownership
        EnsureOwner(challenge, creatorUserId);

        // Verify status is PendingReview
        if (challenge.Status != ChallengeStatus.PendingReview)
        {
            throw new InvalidOperationException(
                $"Cannot publish challenge. Status must be 'PendingReview' but is '{challenge.Status}'");
        }

        // Update to Published
        challenge.Status = ChallengeStatus.Published;
        challenge.PublishedAt = DateTime.UtcNow;
        challenge.UpdatedAt = DateTime.UtcNow;
        await _challengeRepository.UpdateAsync(challenge);

        _logger.LogInformation("Creator {UserId} self-approved and published challenge {ChallengeId}", 
            creatorUserId, challengeId);

        // Return updated challenge
        return new CreatorChallengeDto
        {
            Id = challenge.Id,
            Title = challenge.Title,
            Description = challenge.Description,
            Status = challenge.Status.ToString(),
            CurrentVersionId = challenge.CurrentVersionId,
            CreatedAt = challenge.CreatedAt,
            UpdatedAt = challenge.UpdatedAt,
            Deadline = challenge.Deadline
        };
    }

    public async Task<(List<PublicChallengeDto> items, int totalCount)> GetPublishedChallengesAsync(
        int skip,
        int take,
        string? searchTerm = null,
        string? skillFilter = null)
    {
        var published = await _challengeRepository.GetPublishedAsync();
        
        var filtered = published
            .Where(c => c.CurrentVersionId.HasValue) // Must have active version
            .AsEnumerable();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            filtered = filtered.Where(c =>
                c.Title.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                c.Description.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
        }

        var ordered = filtered
            .OrderByDescending(c => c.PublishedAt ?? c.CreatedAt)
            .ToList();

        var totalCount = ordered.Count;
        var paginated = ordered
            .Skip(skip)
            .Take(take)
            .ToList();

        var dtos = new List<PublicChallengeDto>();
        foreach (var challenge in paginated)
        {
            var version = challenge.CurrentVersion ?? await _versionRepository.GetByIdAsync(challenge.CurrentVersionId.Value);
            if (version is null)
                continue;

            var publicVersion = await MapToPublicVersionDtoAsync(version);
            dtos.Add(new PublicChallengeDto
            {
                Id = challenge.Id,
                Title = challenge.Title,
                Description = challenge.Description,
                DifficultyScore = challenge.DifficultyScore,
                DifficultyLabel = challenge.DifficultyLabel,
                Deadline = challenge.Deadline,
                PublishedAt = challenge.PublishedAt,
                CreatedAt = challenge.CreatedAt,
                CreatedById = challenge.CreatedById,
                ReviewedById = challenge.ReviewedById,
                CurrentVersionId = version.Id,
                ActiveVersion = publicVersion
            });
        }

        return (dtos, totalCount);
    }

    public async Task<PublicChallengeDto?> GetPublicChallengeByIdAsync(Guid id)
    {
        var challenge = await _challengeRepository.GetByIdAsync(id);
        if (challenge is null || challenge.Status != ChallengeStatus.Published || !challenge.CurrentVersionId.HasValue)
        {
            return null;
        }

        var version = challenge.CurrentVersion ?? await _versionRepository.GetByIdAsync(challenge.CurrentVersionId.Value);
        if (version is null)
        {
            return null;
        }

        var publicVersion = await MapToPublicVersionDtoAsync(version);
        return new PublicChallengeDto
        {
            Id = challenge.Id,
            Title = challenge.Title,
            Description = challenge.Description,
            DifficultyScore = challenge.DifficultyScore,
            DifficultyLabel = challenge.DifficultyLabel,
            Deadline = challenge.Deadline,
            PublishedAt = challenge.PublishedAt,
            CreatedAt = challenge.CreatedAt,
            CreatedById = challenge.CreatedById,
            ReviewedById = challenge.ReviewedById,
            CurrentVersionId = version.Id,
            ActiveVersion = publicVersion
        };
    }

    private async Task<PublicVersionDto> MapToPublicVersionDtoAsync(ChallengeVersion version)
    {
        var dto = new PublicVersionDto
        {
            Id = version.Id,
            VersionNumber = version.VersionNumber,
            DifficultyScore = version.DifficultyScore,
            DifficultyLabel = version.DifficultyLabel,
            CreatedAt = version.CreatedAt
        };

        // Parse skill weights
        if (!string.IsNullOrEmpty(version.SkillWeightMapping))
        {
            try
            {
                dto.SkillWeights = JsonSerializer.Deserialize<Dictionary<string, decimal>>(version.SkillWeightMapping) ?? new();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse skill weights for version {VersionId}", version.Id);
            }
        }

        // Load criteria
        try
        {
            var criteria = await _challengeCriteriaRepository.GetByVersionAsync(version.Id);
            dto.Criteria = criteria
                .Select(c => new PublicCriteriaDto
                {
                    Id = c.Id,
                    Name = c.Criteria?.Name ?? "Unknown",
                    Description = c.Criteria?.Description ?? string.Empty,
                    MaxScore = 100,
                    DisplayOrder = 0
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load criteria for version {VersionId}", version.Id);
            dto.Criteria = new();
        }

        return dto;
    }

    private async Task<IEnumerable<ChallengeEntity>> ResolveChallengesAsync(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
        {
            return await _challengeRepository.GetAllAsync();
        }

        if (!Enum.TryParse<ChallengeStatus>(status, ignoreCase: true, out var parsedStatus))
        {
            return await _challengeRepository.GetAllAsync();
        }

        return parsedStatus switch
        {
            ChallengeStatus.Published => await _challengeRepository.GetPublishedAsync(),
            ChallengeStatus.Expired => await _challengeRepository.GetExpiredAsync(),
            _ => await _challengeRepository.GetByStatusAsync(parsedStatus)
        };
    }

    private static ChallengeDto MapToDto(ChallengeEntity challenge)
    {
        return new ChallengeDto
        {
            Id = challenge.Id,
            Title = challenge.Title,
            Description = challenge.Description,
            Status = challenge.Status.ToString(),
            CreatedAt = challenge.CreatedAt,
            CreatedById = challenge.CreatedById,
            ReviewedById = challenge.ReviewedById,
            Deadline = challenge.Deadline,
            PublishedAt = challenge.PublishedAt
        };
    }

    private void EnsureOwner(ChallengeEntity challenge, int userId)
    {
        if (challenge.CreatedById != userId)
        {
            throw new UnauthorizedAccessException("You do not have permission to modify this challenge.");
        }
    }

    private async Task<int> GetNextVersionNumberAsync(Guid challengeId)
    {
        var versions = await _versionRepository.GetVersionsByChallengeAsync(challengeId);
        var latestVersionNumber = versions.FirstOrDefault()?.VersionNumber ?? 0;
        return latestVersionNumber + 1;
    }

    private async Task EnsureSkillsFromAnalysisAsync(ChallengeEntity challenge, ChallengeAnalysisResult analysis)
    {
        foreach (var skillPair in analysis.SkillWeights.OrderByDescending(pair => pair.Value))
        {
            var skillName = skillPair.Key;
            var weight = skillPair.Value;
            var slug = NormalizeSlug(skillName);
            var existing = await _skillRepository.GetBySlugAsync(slug);

            if (existing is null)
            {
                existing = new Skill
                {
                    Id = Guid.NewGuid(),
                    Name = skillName,
                    Slug = slug,
                    Description = $"AI-generated skill from challenge '{challenge.Title}'",
                    CategoryId = null,
                    IsApproved = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _skillRepository.AddAsync(existing);
                _logger.LogInformation("Created AI skill {SkillName} ({SkillId}) with weight {Weight}", skillName, existing.Id, weight);
            }
            else if (!existing.IsApproved)
            {
                existing.IsApproved = true;
                existing.UpdatedAt = DateTime.UtcNow;
                await _skillRepository.UpdateAsync(existing);
            }
        }
    }

    private async Task PersistCriteriaModelAsync(ChallengeVersion version, ChallengeAnalysisResult analysis)
    {
        var criteriaNames = analysis.EvaluationCriteria?
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? new List<string>();

        if (!criteriaNames.Any())
        {
            criteriaNames = analysis.SkillWeights.Keys.ToList();
        }

        if (!criteriaNames.Any())
        {
            _logger.LogWarning("No criteria/skills extracted for version {VersionId}; criteria tables not populated", version.Id);
            return;
        }

        var challengeCriteriaRows = new List<ChallengeCriteria>();
        var criteriaSkillRows = new List<CriteriaSkillMapping>();
        var criteriaWeight = Math.Round(1m / criteriaNames.Count, 4);

        foreach (var criteriaName in criteriaNames)
        {
            var criteriaEntity = await ResolveOrCreateCriteriaAsync(criteriaName);
            challengeCriteriaRows.Add(new ChallengeCriteria
            {
                Id = Guid.NewGuid(),
                ChallengeVersionId = version.Id,
                CriteriaId = criteriaEntity.Id,
                Weight = criteriaWeight,
                VersionedAt = DateTime.UtcNow
            });

            var mappedSkill = await ResolveSkillForCriteriaAsync(criteriaName, analysis.SkillWeights);
            if (mappedSkill is null)
            {
                _logger.LogWarning(
                    "No mapped skill found for criteria '{CriteriaName}' in version {VersionId}",
                    criteriaName,
                    version.Id);
                continue;
            }

            var mappingWeight = analysis.SkillWeights.TryGetValue(mappedSkill.Name, out var skillWeight)
                ? skillWeight
                : 1m;

            criteriaSkillRows.Add(new CriteriaSkillMapping
            {
                Id = Guid.NewGuid(),
                CriteriaId = criteriaEntity.Id,
                SkillId = mappedSkill.Id,
                Weight = mappingWeight,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _challengeCriteriaRepository.AddRangeAsync(challengeCriteriaRows);
        await _criteriaSkillMappingRepository.AddRangeAsync(criteriaSkillRows);

        _logger.LogInformation(
            "Persisted criteria model for version {VersionId}: challengeCriteria={ChallengeCriteriaCount}, criteriaSkillMappings={CriteriaSkillCount}",
            version.Id,
            challengeCriteriaRows.Count,
            criteriaSkillRows.Count);
    }

    private async Task<EvaluationCriteria> ResolveOrCreateCriteriaAsync(string criteriaName)
    {
        var existing = await _evaluationCriteriaRepository.GetByNameAsync(criteriaName);
        if (existing is not null)
        {
            return existing;
        }

        var created = new EvaluationCriteria
        {
            Id = Guid.NewGuid(),
            Name = criteriaName,
            Description = $"AI-generated criteria: {criteriaName}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _evaluationCriteriaRepository.AddAsync(created);
        return created;
    }

    private async Task<Skill?> ResolveSkillForCriteriaAsync(string criteriaName, Dictionary<string, decimal> skillWeights)
    {
        var normalizedCriteria = NormalizeSlug(criteriaName);
        var candidateNames = skillWeights
            .OrderByDescending(x => x.Value)
            .Select(x => x.Key)
            .ToList();

        var matchedName = candidateNames.FirstOrDefault(skillName =>
        {
            var normalizedSkill = NormalizeSlug(skillName);
            return normalizedSkill.Contains(normalizedCriteria, StringComparison.OrdinalIgnoreCase)
                   || normalizedCriteria.Contains(normalizedSkill, StringComparison.OrdinalIgnoreCase);
        });

        if (matchedName is null)
        {
            matchedName = candidateNames.FirstOrDefault();
        }

        if (matchedName is null)
        {
            return null;
        }

        return await _skillRepository.GetBySlugAsync(NormalizeSlug(matchedName));
    }

    private static string NormalizeSlug(string value)
    {
        return string.Join(
            "-",
            value
                .Trim()
                .ToLowerInvariant()
                .Split(new[] { ' ', '\t', '\r', '\n', '/', '\\', '+', '.', ',', ':', ';', '(', ')' }, StringSplitOptions.RemoveEmptyEntries));
    }
}

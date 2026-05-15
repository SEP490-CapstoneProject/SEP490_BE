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
    private readonly IGeminiAIService _geminiAIService;
    private readonly IEventPublisher _eventPublisher;
    private readonly IActorResolverClient _actorResolverClient;
    private readonly ILogger<ChallengeService> _logger;

    public ChallengeService(
        IChallengeRepository repository,
        IChallengeVersionRepository versionRepository,
        ISkillRepository skillRepository,
        IGeminiAIService geminiAIService,
        IEventPublisher eventPublisher,
        IActorResolverClient actorResolverClient,
        ILogger<ChallengeService> logger)
    {
        _challengeRepository = repository;
        _versionRepository = versionRepository;
        _skillRepository = skillRepository;
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
            CreatedById = ResolveActorGuid(userId),
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
        return challenge is null ? null : MapToDto(challenge);
    }

    public async Task<List<ChallengeDto>> ListChallengesAsync(int pageSize = 20, int? cursor = null)
    {
        var challenges = await _challengeRepository.GetAllAsync();
        return challenges
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
        challenge.ReviewedById = ResolveActorGuid(adminId);
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
        challenge.ReviewedById = ResolveActorGuid(adminId);
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
            var actorGuid = ResolveActorGuid(userId.Value);
            challenges = challenges.Where(challenge => challenge.CreatedById == actorGuid);
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
            Deadline = challenge.Deadline,
            PublishedAt = challenge.PublishedAt
        };
    }

    private static Guid ResolveActorGuid(int userId)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(userId.ToString()));
        Span<byte> guidBytes = stackalloc byte[16];
        bytes.AsSpan(0, 16).CopyTo(guidBytes);
        return new Guid(guidBytes);
    }

    private void EnsureOwner(ChallengeEntity challenge, int userId)
    {
        var actorGuid = ResolveActorGuid(userId);
        if (challenge.CreatedById != actorGuid)
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

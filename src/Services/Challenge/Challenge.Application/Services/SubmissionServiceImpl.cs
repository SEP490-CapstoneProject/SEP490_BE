using System.Security.Cryptography;
using System.Text;
using Challenge.Application.DTOs;
using Challenge.Application.Interfaces;
using Challenge.Application.Clients;
using Challenge.Domain.Entities;
using Challenge.Domain.Enums;
using Challenge.Domain.Repositories;

namespace Challenge.Application.Services;

public class SubmissionService : ISubmissionService
{
    private readonly ISubmissionRepository _submissionRepository;
    private readonly IChallengeRepository _challengeRepository;
    private readonly IChallengeVersionRepository _versionRepository;
    private readonly IGradingService _gradingService;
    private readonly ISkillPointService _skillPointService;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<SubmissionService> _logger;

    public SubmissionService(
        ISubmissionRepository submissionRepository,
        IChallengeRepository challengeRepository,
        IChallengeVersionRepository versionRepository,
        IGradingService gradingService,
        ISkillPointService skillPointService,
        IEventPublisher eventPublisher,
        ILogger<SubmissionService> logger)
    {
        _submissionRepository = submissionRepository;
        _challengeRepository = challengeRepository;
        _versionRepository = versionRepository;
        _gradingService = gradingService;
        _skillPointService = skillPointService;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<SubmissionDto> SubmitSolutionAsync(Guid challengeId, SubmitSolutionDto request, int userId)
    {
        ArgumentNullException.ThrowIfNull(request);

        var challenge = await _challengeRepository.GetByIdAsync(challengeId);
        if (challenge is null)
        {
            throw new KeyNotFoundException($"Challenge {challengeId} not found");
        }

        if (challenge.Status != ChallengeStatus.Published)
        {
            throw new UnauthorizedAccessException("Challenge must be published before submission.");
        }

        var actorGuid = ResolveActorGuid(userId);
        var version = challenge.CurrentVersion ?? (await _versionRepository.GetVersionsByChallengeAsync(challengeId)).FirstOrDefault();
        if (version is null)
        {
            throw new InvalidOperationException($"Challenge {challengeId} does not have an analyzed version.");
        }

        var now = DateTime.UtcNow;
        var submission = new ChallengeSubmission
        {
            Id = Guid.NewGuid(),
            ChallengeId = challengeId,
            UserId = actorGuid,
            SubmissionContent = request.Content ?? string.Empty,
            GithubUrl = request.GithubUrl ?? string.Empty,
            OverallScore = 0m,
            AiFeedback = string.Empty,
            Status = SubmissionStatus.Pending,
            VersionSnapshotId = version.Id,
            VersionId = version.Id,
            AttemptCount = await _submissionRepository.GetAttemptCountAsync(actorGuid, challengeId) + 1,
            CreatedAt = now,
            UpdatedAt = now,
            GradedAt = null
        };

        await _submissionRepository.AddAsync(submission);
        await GradeAndPersistAsync(submission, version);

        _logger.LogInformation("Submission {SubmissionId} created for challenge {ChallengeId}", submission.Id, challengeId);
        return Map(submission);
    }

    public async Task<SubmissionDto?> GetSubmissionByIdAsync(Guid id, int? currentUserId)
    {
        var submission = await _submissionRepository.GetByIdAsync(id);
        if (submission is null)
        {
            return null;
        }

        if (currentUserId.HasValue && submission.UserId != ResolveActorGuid(currentUserId.Value))
        {
            return null;
        }

        return Map(submission);
    }

    public async Task<List<SubmissionDto>> GetUserSubmissionsAsync(int userId, Guid? challengeId = null)
    {
        var actorGuid = ResolveActorGuid(userId);
        var submissions = challengeId.HasValue
            ? await _submissionRepository.GetByUserAndChallengeAsync(actorGuid, challengeId.Value)
            : await _submissionRepository.GetByUserAsync(actorGuid);

        return submissions.Select(Map).ToList();
    }

    public async Task<List<SubmissionDto>> GetChallengeSubmissionsAsync(Guid challengeId)
    {
        var submissions = await _submissionRepository.GetByChallengeAsync(challengeId);
        return submissions.Select(Map).ToList();
    }

    public async Task<SubmissionDto> GradeSubmissionAsync(Guid id)
    {
        var submission = await _submissionRepository.GetByIdAsync(id);
        if (submission is null)
        {
            throw new KeyNotFoundException($"Submission {id} not found");
        }

        var version = await _versionRepository.GetByIdAsync(submission.VersionSnapshotId);
        await GradeAndPersistAsync(submission, version);
        return Map(submission);
    }

    public async Task<(List<SubmissionDto> items, int totalCount)> GetSubmissionsPagedAsync(
        int skip,
        int take,
        string? status = null,
        int? userId = null)
    {
        IEnumerable<ChallengeSubmission> submissions = await _submissionRepository.GetAllAsync();

        if (userId.HasValue)
        {
            var actorGuid = ResolveActorGuid(userId.Value);
            submissions = submissions.Where(s => s.UserId == actorGuid);
        }

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<SubmissionStatus>(status, true, out var parsedStatus))
        {
            submissions = submissions.Where(s => s.Status == parsedStatus);
        }

        var ordered = submissions.OrderByDescending(s => s.CreatedAt).ToList();
        var totalCount = ordered.Count;
        var items = ordered.Skip(skip).Take(take).Select(Map).ToList();
        return (items, totalCount);
    }

    private async Task GradeAndPersistAsync(ChallengeSubmission submission, ChallengeVersion version)
    {
        var grading = await _gradingService.GradeSubmissionAsync(submission, version);

        submission.OverallScore = (decimal)grading.overallScore;
        submission.AiFeedback = grading.feedback ?? string.Empty;
        submission.Status = SubmissionStatus.Graded;
        submission.GradedAt = DateTime.UtcNow;
        submission.UpdatedAt = DateTime.UtcNow;

        await _submissionRepository.UpdateAsync(submission);
    }

    private static SubmissionDto Map(ChallengeSubmission submission)
    {
        return new SubmissionDto
        {
            Id = submission.Id,
            ChallengeId = submission.ChallengeId,
            UserId = submission.UserId,
            Status = submission.Status.ToString(),
            OverallScore = submission.OverallScore,
            CreatedAt = submission.CreatedAt,
            GradedAt = submission.GradedAt
        };
    }

    private static Guid ResolveActorGuid(int userId)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(userId.ToString()));
        Span<byte> guidBytes = stackalloc byte[16];
        bytes.AsSpan(0, 16).CopyTo(guidBytes);
        return new Guid(guidBytes);
    }
}

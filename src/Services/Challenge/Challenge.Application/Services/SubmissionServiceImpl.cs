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
    private readonly IActorResolverClient _actorResolverClient;
    private readonly ILogger<SubmissionService> _logger;

    public SubmissionService(
        ISubmissionRepository submissionRepository,
        IChallengeRepository challengeRepository,
        IChallengeVersionRepository versionRepository,
        IGradingService gradingService,
        ISkillPointService skillPointService,
        IEventPublisher eventPublisher,
        IActorResolverClient actorResolverClient,
        ILogger<SubmissionService> logger)
    {
        _submissionRepository = submissionRepository;
        _challengeRepository = challengeRepository;
        _versionRepository = versionRepository;
        _gradingService = gradingService;
        _skillPointService = skillPointService;
        _eventPublisher = eventPublisher;
        _actorResolverClient = actorResolverClient;
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
            UserId = userId,
            SubmissionContent = request.Content ?? string.Empty,
            GithubUrl = request.GithubUrl ?? string.Empty,
            OverallScore = 0m,
            AiFeedback = string.Empty,
            Status = SubmissionStatus.Pending,
            VersionSnapshotId = version.Id,
            VersionId = version.Id,
            AttemptCount = await _submissionRepository.GetAttemptCountAsync(userId, challengeId) + 1,
            CreatedAt = now,
            UpdatedAt = now,
            GradedAt = null
        };

        await _submissionRepository.AddAsync(submission);

        var grading = await GradeAndPersistAsync(submission, version);

        // Calculate and award skill points using caller's userId
        var skillPoints = await _skillPointService.CalculateSkillPointsAsync(submission, version, grading.criteriaScores);
        await _skillPointService.AwardPointsAsync(userId, skillPoints, submission.Id, "Challenge completion");

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

        if (currentUserId.HasValue && submission.UserId != currentUserId.Value)
        {
            return null;
        }

        return Map(submission);
    }

    public async Task<List<SubmissionDto>> GetUserSubmissionsAsync(int userId, Guid? challengeId = null)
    {
        var submissions = challengeId.HasValue
            ? await _submissionRepository.GetByUserAndChallengeAsync(userId, challengeId.Value)
            : await _submissionRepository.GetByUserAsync(userId);

        return submissions.Select(Map).ToList();
    }

    public async Task<ParticipantSubmittedChallengeListResponseDto> GetSubmittedChallengesAsync(
        int userId,
        int skip,
        int take)
    {
        var submissions = await _submissionRepository.GetByUserAsync(userId);
        var latestSubmissions = submissions
            .OrderByDescending(s => s.CreatedAt)
            .GroupBy(s => s.ChallengeId)
            .Select(group => group.First())
            .OrderByDescending(s => s.CreatedAt)
            .ToList();

        var totalCount = latestSubmissions.Count;
        var paginated = latestSubmissions
            .Skip(skip)
            .Take(take)
            .ToList();

        var challengeIds = paginated.Select(s => s.ChallengeId).Distinct().ToList();
        var challenges = await _challengeRepository.GetByIdsAsync(challengeIds);
        var challengeMap = challenges.ToDictionary(challenge => challenge.Id);

        var items = paginated
            .Where(submission => challengeMap.ContainsKey(submission.ChallengeId))
            .Select(submission =>
            {
                var challenge = challengeMap[submission.ChallengeId];
                return new ParticipantSubmittedChallengeDto
                {
                    ChallengeId = challenge.Id,
                    ChallengeTitle = challenge.Title,
                    ChallengeDescription = challenge.Description,
                    ChallengeDeadline = challenge.Deadline,
                    PublishedAt = challenge.PublishedAt,
                    LatestSubmissionId = submission.Id,
                    LatestSubmissionStatus = submission.Status.ToString(),
                    LatestSubmittedAt = submission.CreatedAt,
                    LatestEvaluationScore = submission.OverallScore > 0 ? submission.OverallScore : null,
                    LatestEvaluationStatus = submission.GradedAt.HasValue ? "Completed" : "Pending",
                    LatestEvaluatedAt = submission.GradedAt,
                    LatestFeedback = submission.AiFeedback,
                    AttemptCount = submission.AttemptCount
                };
            })
            .ToList();

        return new ParticipantSubmittedChallengeListResponseDto
        {
            Items = items,
            TotalCount = totalCount,
            Skip = skip,
            Take = take
        };
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
            submissions = submissions.Where(s => s.UserId == userId.Value);
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

    private async Task<(double overallScore, Dictionary<string, double> criteriaScores, string feedback)> GradeAndPersistAsync(ChallengeSubmission submission, ChallengeVersion version)
    {
        var grading = await _gradingService.GradeSubmissionAsync(submission, version);

        submission.OverallScore = (decimal)grading.overallScore;
        submission.AiFeedback = grading.feedback ?? string.Empty;
        submission.Status = SubmissionStatus.Graded;
        submission.GradedAt = DateTime.UtcNow;
        submission.UpdatedAt = DateTime.UtcNow;

        await _submissionRepository.UpdateAsync(submission);

        return grading;
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
            AiFeedback = submission.AiFeedback,
            CreatedAt = submission.CreatedAt,
            GradedAt = submission.GradedAt
        };
    }

    public async Task<SubmissionListResponseDto> GetChallengeSubmissionsWithUserInfoAsync(
        Guid challengeId,
        int skip,
        int take)
    {
        var challenge = await _challengeRepository.GetByIdAsync(challengeId);
        if (challenge is null)
        {
            throw new KeyNotFoundException($"Challenge {challengeId} not found");
        }

        // Get all submissions for this challenge
        var submissions = await _submissionRepository.GetByChallengeAsync(challenge.Id);
        var totalCount = submissions.Count();

        // Apply pagination and sorting
        var paginated = submissions
            .OrderByDescending(s => s.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToList();

        // Collect unique user IDs
        var userIds = paginated
            .Select(s => s.UserId)
            .Distinct()
            .ToList();

        // Batch fetch user info from UserProfile service
        var userInfoMap = new Dictionary<int, UserInfoDto>();
        if (userIds.Any())
        {
            userInfoMap = await _actorResolverClient.GetUsersByIdsAsync(userIds);
        }

        // Map to DTOs with user info
        var items = paginated.Select(s => new SubmissionWithUserDto
        {
            Id = s.Id,
            ChallengeId = s.ChallengeId,
            UserId = s.UserId,
            UserName = userInfoMap.TryGetValue(s.UserId, out var user)
                ? user.FullName ?? $"User {s.UserId}"
                : $"User {s.UserId}",
            UserEmail = userInfoMap.TryGetValue(s.UserId, out var user2)
                ? user2.Email ?? ""
                : "",
            UserAvatar = userInfoMap.TryGetValue(s.UserId, out var user3)
                ? user3.Avatar ?? ""
                : "",
            SubmissionStatus = s.Status.ToString(),
            SubmittedAt = s.CreatedAt,
            SubmissionContent = s.SubmissionContent,
            GitHubLink = s.GithubUrl,
            EvaluationScore = s.OverallScore > 0 ? s.OverallScore : null,
            EvaluationStatus = s.GradedAt.HasValue ? "Completed" : "Pending",
            EvaluatedAt = s.GradedAt,
            Feedback = s.AiFeedback,
            AttemptCount = s.AttemptCount
        }).ToList();

        return new SubmissionListResponseDto
        {
            Items = items,
            TotalCount = totalCount,
            Skip = skip,
            Take = take
        };
    }

    public async Task<ParticipantSubmissionListResponseDto> GetUserSubmissionsForChallengeAsync(
        Guid challengeId,
        int userId,
        int skip,
        int take)
    {
        var challenge = await _challengeRepository.GetByIdAsync(challengeId);
        if (challenge is null)
        {
            throw new KeyNotFoundException($"Challenge {challengeId} not found");
        }

        // Only allow if challenge is published
        if (challenge.Status != ChallengeStatus.Published)
        {
            throw new InvalidOperationException("Challenge is not published");
        }

        // Get submissions for user and challenge (userId is now int directly)
        var submissions = await _submissionRepository.GetByUserAndChallengeAsync(userId, challengeId);
        var totalCount = submissions.Count();

        // Apply pagination and sorting
        var paginated = submissions
            .OrderByDescending(s => s.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToList();

        // Map to DTOs
        var items = paginated.Select(s => new ParticipantSubmissionDto
        {
            Id = s.Id,
            ChallengeId = s.ChallengeId,
            ChallengeTitle = challenge.Title,
            SubmissionStatus = s.Status.ToString(),
            SubmittedAt = s.CreatedAt,
            SubmissionContent = s.SubmissionContent,
            GitHubLink = s.GithubUrl,
            EvaluationScore = s.OverallScore > 0 ? s.OverallScore : null,
            EvaluationStatus = s.GradedAt.HasValue ? "Completed" : "Pending",
            EvaluatedAt = s.GradedAt,
            Feedback = s.AiFeedback,
            AttemptCount = s.AttemptCount
        }).ToList();

        return new ParticipantSubmissionListResponseDto
        {
            Items = items,
            TotalCount = totalCount,
            Skip = skip,
            Take = take
        };
    }
}

using Challenge.Application.DTOs;
using Challenge.Application.Interfaces;
using Challenge.Application.Clients;

namespace Challenge.Application.Services;

public class SubmissionService : ISubmissionService
{
    private readonly ILogger<SubmissionService> _logger;

    public SubmissionService(
        ISubmissionRepository submissionRepository,
        IChallengeRepository challengeRepository,
        IGradingService gradingService,
        ISkillPointService skillPointService,
        IEventPublisher eventPublisher,
        ILogger<SubmissionService> logger)
    {
        _logger = logger;
    }

    public Task<SubmissionDto> SubmitSolutionAsync(int challengeId, SubmitSolutionDto request, int userId)
        => Task.FromResult(new SubmissionDto { Id = 1, ChallengeId = challengeId, UserId = userId, Status = "Pending", CreatedAt = DateTime.UtcNow });

    public Task<SubmissionDto?> GetSubmissionByIdAsync(int id, int? currentUserId)
        => Task.FromResult<SubmissionDto?>(null);

    public Task<List<SubmissionDto>> GetUserSubmissionsAsync(int userId, int? challengeId = null)
        => Task.FromResult(new List<SubmissionDto>());

    public Task<List<SubmissionDto>> GetChallengeSubmissionsAsync(int challengeId)
        => Task.FromResult(new List<SubmissionDto>());

    public Task<SubmissionDto> GradeSubmissionAsync(int id)
        => Task.FromResult(new SubmissionDto { Id = id, Status = "Graded", OverallScore = 8.5m, CreatedAt = DateTime.UtcNow });

    public Task<(List<SubmissionDto> items, int totalCount)> GetSubmissionsPagedAsync(int skip, int take, string? status = null, int? userId = null)
        => Task.FromResult((new List<SubmissionDto>(), 0));
}

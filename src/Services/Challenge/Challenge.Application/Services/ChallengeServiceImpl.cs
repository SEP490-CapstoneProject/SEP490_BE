using Challenge.Application.Clients;
using Challenge.Application.DTOs;
using Challenge.Application.Interfaces;

namespace Challenge.Application.Services;

public class ChallengeService : IChallengeService
{
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<ChallengeService> _logger;

    public ChallengeService(
        IChallengeRepository repository,
        IChallengeVersionRepository versionRepository,
        IEventPublisher eventPublisher,
        IActorResolverClient actorResolverClient,
        ILogger<ChallengeService> logger)
    {
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public Task<ChallengeDto> CreateChallengeAsync(CreateChallengeDto request, int userId)
        => Task.FromResult(new ChallengeDto
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            Status = "Draft",
            CreatedAt = DateTime.UtcNow,
            Deadline = request.Deadline
        });

    public Task<ChallengeDto?> GetChallengeByIdAsync(int id, int? currentUserId)
        => Task.FromResult<ChallengeDto?>(null);

    public Task<List<ChallengeDto>> ListChallengesAsync(int pageSize = 20, int? cursor = null)
        => Task.FromResult(new List<ChallengeDto>());

    public Task<ChallengeDto> UpdateChallengeAsync(int id, UpdateChallengeDto request, int userId)
        => Task.FromResult(new ChallengeDto { Id = Guid.NewGuid(), Title = request.Title ?? string.Empty });

    public Task DeleteChallengeAsync(int id, int userId) => Task.CompletedTask;

    public Task<ChallengeDto> SubmitForReviewAsync(int id, int userId)
        => Task.FromResult(new ChallengeDto { Id = Guid.NewGuid(), Status = "PendingReview" });

    public Task<ChallengeDto> ApproveChallengeAsync(int id, int adminId)
        => Task.FromResult(new ChallengeDto { Id = Guid.NewGuid(), Status = "Published" });

    public Task<ChallengeDto> RejectChallengeAsync(int id, string reason, int adminId)
        => Task.FromResult(new ChallengeDto { Id = Guid.NewGuid(), Status = "Rejected" });

    public Task<(List<ChallengeDto> items, int totalCount)> GetChallengesPagedAsync(
        int skip,
        int take,
        string? status = null,
        int? userId = null)
        => Task.FromResult((new List<ChallengeDto>(), 0));
}

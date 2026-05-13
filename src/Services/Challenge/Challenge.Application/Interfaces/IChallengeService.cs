using Challenge.Application.DTOs;
using Challenge.Domain.Entities;

namespace Challenge.Application.Interfaces;

/// <summary>
/// Service for managing challenge lifecycle: creation, moderation, publication
/// </summary>
public interface IChallengeService
{
    // CRUD Operations
    Task<ChallengeDto> CreateChallengeAsync(CreateChallengeDto request, int userId);
    Task<ChallengeDto?> GetChallengeByIdAsync(int id, int? currentUserId);
    Task<List<ChallengeDto>> ListChallengesAsync(int pageSize = 20, int? cursor = null);
    Task<ChallengeDto> UpdateChallengeAsync(int id, UpdateChallengeDto request, int userId);
    Task DeleteChallengeAsync(int id, int userId);

    // Moderation Workflow
    Task<ChallengeDto> SubmitForReviewAsync(int id, int userId);
    Task<ChallengeDto> ApproveChallengeAsync(int id, int adminId);
    Task<ChallengeDto> RejectChallengeAsync(int id, string reason, int adminId);

    // Query operations
    Task<(List<ChallengeDto> items, int totalCount)> GetChallengesPagedAsync(
        int skip, 
        int take, 
        string? status = null,
        int? userId = null);
}

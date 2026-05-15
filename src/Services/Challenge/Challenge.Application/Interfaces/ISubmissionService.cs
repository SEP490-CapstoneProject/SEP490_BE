using Challenge.Application.DTOs;

namespace Challenge.Application.Interfaces;

/// <summary>
/// Service for managing user submissions and grading
/// </summary>
public interface ISubmissionService
{
    // Submit and retrieve
    Task<SubmissionDto> SubmitSolutionAsync(int challengeId, SubmitSolutionDto request, int userId);
    Task<SubmissionDto?> GetSubmissionByIdAsync(int id, int? currentUserId);
    Task<List<SubmissionDto>> GetUserSubmissionsAsync(int userId, int? challengeId = null);
    Task<List<SubmissionDto>> GetChallengeSubmissionsAsync(int challengeId);

    // Grading
    Task<SubmissionDto> GradeSubmissionAsync(int id);
    Task<(List<SubmissionDto> items, int totalCount)> GetSubmissionsPagedAsync(
        int skip,
        int take,
        string? status = null,
        int? userId = null);
}

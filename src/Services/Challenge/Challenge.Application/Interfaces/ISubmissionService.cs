using Challenge.Application.DTOs;

namespace Challenge.Application.Interfaces;

/// <summary>
/// Service for managing user submissions and grading
/// </summary>
public interface ISubmissionService
{
    // Submit and retrieve
    Task<SubmissionDto> SubmitSolutionAsync(Guid challengeId, SubmitSolutionDto request, int userId);
    Task<SubmissionDto?> GetSubmissionByIdAsync(Guid id, int? currentUserId);
    Task<List<SubmissionDto>> GetUserSubmissionsAsync(int userId, Guid? challengeId = null);
    Task<List<SubmissionDto>> GetChallengeSubmissionsAsync(Guid challengeId);

    // Grading
    Task<SubmissionDto> GradeSubmissionAsync(Guid id);
    Task<(List<SubmissionDto> items, int totalCount)> GetSubmissionsPagedAsync(
        int skip,
        int take,
        string? status = null,
        int? userId = null);

    // Submission management APIs
    Task<SubmissionListResponseDto> GetChallengeSubmissionsWithUserInfoAsync(
        Guid challengeId,
        int skip,
        int take);

    Task<ParticipantSubmissionListResponseDto> GetUserSubmissionsForChallengeAsync(
        Guid challengeId,
        int userId,
        int skip,
        int take);
}

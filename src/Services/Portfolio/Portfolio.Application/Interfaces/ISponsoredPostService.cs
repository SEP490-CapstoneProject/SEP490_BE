using Portfolio.Application.DTOs;
using Portfolio.Domain.Entities;

namespace Portfolio.Application.Interfaces;

/// <summary>
/// Service for creating and managing sponsored posts.
/// Handles point spending validation, duration calculation, and post creation.
/// </summary>
public interface ISponsoredPostService
{
    /// <summary>
    /// Creates a sponsored post, spending reward points in the process.
    /// Validates recruiter has sufficient points and post content is valid.
    /// </summary>
    Task<SponsoredPostDto> CreateSponsoredPostAsync(int recruiterId, CreateSponsoredPostRequest request);

    /// <summary>
    /// Gets sponsored post by ID.
    /// </summary>
    Task<SponsoredPostDto?> GetPostAsync(int postId);

    /// <summary>
    /// Gets all active sponsored posts (paginated).
    /// </summary>
    Task<List<SponsoredPostDto>> GetActivePostsAsync(int skip = 0, int take = 20);

    /// <summary>
    /// Gets all sponsored posts created by recruiter.
    /// </summary>
    Task<List<SponsoredPostDto>> GetRecruiterPostsAsync(int recruiterId);

    /// <summary>
    /// Deletes a sponsored post (only creator or admin).
    /// </summary>
    Task DeletePostAsync(int postId, int userId, bool isAdmin);

    /// <summary>
    /// Pauses a sponsored post (stops it from being displayed).
    /// </summary>
    Task PausePostAsync(int postId, int userId, bool isAdmin);

    /// <summary>
    /// Resumes a paused sponsored post.
    /// </summary>
    Task ResumePostAsync(int postId, int userId, bool isAdmin);

    /// <summary>
    /// Records a view for a sponsored post (for analytics).
    /// </summary>
    Task RecordViewAsync(int postId);

    /// <summary>
    /// Records a click for a sponsored post (for analytics).
    /// </summary>
    Task RecordClickAsync(int postId);
}

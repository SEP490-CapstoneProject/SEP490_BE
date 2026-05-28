using Portfolio.Application.DTOs;
using Portfolio.Domain.Entities;

namespace Portfolio.Application.Interfaces;

/// <summary>
/// Repository for SponsoredPost entity - manages sponsored content created by recruiters.
/// </summary>
public interface ISponsoredPostRepository
{
    /// <summary>
    /// Gets sponsored post by ID (active/non-deleted only by default).
    /// </summary>
    Task<SponsoredPost?> GetByIdAsync(int id);

    /// <summary>
    /// Gets all sponsored posts created by recruiter.
    /// </summary>
    Task<List<SponsoredPost>> GetByCreatorAsync(int createdBy);

    /// <summary>
    /// Gets all active sponsored posts currently visible.
    /// </summary>
    Task<List<SponsoredPost>> GetActivePostsAsync();

    /// <summary>
    /// Gets active sponsored posts with pagination.
    /// </summary>
    Task<List<SponsoredPost>> GetActivePostsPagedAsync(int skip, int take);

    /// <summary>
    /// Creates new sponsored post.
    /// </summary>
    Task<SponsoredPost> CreateAsync(SponsoredPost post);

    /// <summary>
    /// Updates existing sponsored post.
    /// </summary>
    Task<SponsoredPost> UpdateAsync(SponsoredPost post);

    /// <summary>
    /// Marks post as deleted.
    /// </summary>
    Task DeleteAsync(int id);

    /// <summary>
    /// Gets count of active posts for recruiter.
    /// </summary>
    Task<int> GetActivePostCountAsync(int createdBy);
}

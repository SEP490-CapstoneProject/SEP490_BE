using Community.Application.DTOs;

namespace Community.Application.Clients;

public interface IUserInfoClient
{
    /// <summary>
    /// Batch fetch author info for multiple userIds.
    /// Returns a dictionary keyed by userId.
    /// If a userId has no profile, it gets a fallback AuthorDto.
    /// </summary>
    Task<Dictionary<int, AuthorDto>> GetAuthorsBatchAsync(IEnumerable<int> userIds);
}

using Connection.Application.DTOs;

namespace Connection.Application.Interfaces;

public interface IUserProfileResolver
{
    Task<NotificationActorDto?> ResolveAsync(int userId, CancellationToken cancellationToken = default);
}

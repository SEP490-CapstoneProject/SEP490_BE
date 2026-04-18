namespace Notification.Application.Interfaces;

public interface IRecipientResolverClient
{
    Task<IReadOnlyList<string>> GetActiveUserIdsByRolesAsync(IEnumerable<string> roles, CancellationToken cancellationToken = default);
}

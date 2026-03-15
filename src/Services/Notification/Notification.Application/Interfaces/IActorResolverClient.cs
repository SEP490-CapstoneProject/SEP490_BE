using Notification.Application.DTOs;

namespace Notification.Application.Interfaces;

public interface IActorResolverClient
{
    Task<ActorDto?> ResolveActorAsync(string actorId, string actorType);
}

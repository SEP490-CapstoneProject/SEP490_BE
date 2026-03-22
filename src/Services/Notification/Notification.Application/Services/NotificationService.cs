using Notification.Application.DTOs;
using Notification.Application.Interfaces;
using Notification.Domain.Entities;

namespace Notification.Application.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _repo;
    private readonly IActorResolverClient _actorResolver;

    public NotificationService(INotificationRepository repo, IActorResolverClient actorResolver)
    {
        _repo = repo;
        _actorResolver = actorResolver;
    }

    public async Task<CursorPagedResult<UserNotificationDto>> GetNotificationsAsync(string userId, int? cursor, int limit)
    {
        var (items, nextCursor) = await _repo.GetNotificationsAsync(userId, cursor, limit);

        var actorMap = new Dictionary<string, ActorDto?>();
        var uniqueActors = items
            .Where(n => n.ActorId != null && n.ActorType != "SYSTEM")
            .Select(n => (n.ActorId!, n.ActorType))
            .Distinct()
            .ToList();

        foreach (var (actorId, actorType) in uniqueActors)
        {
            if (!actorMap.ContainsKey(actorId))
                actorMap[actorId] = await _actorResolver.ResolveActorAsync(actorId, actorType);
        }

        var dtos = items.Select(n => new UserNotificationDto
        {
            Id = n.Id,
            UserId = n.UserId,
            Title = n.Title,
            Content = n.Content,
            Type = n.Type,
            ObjectId = n.ObjectId,
            Actor = n.ActorId != null ? actorMap.GetValueOrDefault(n.ActorId) : null,
            CreatedAt = n.CreatedAt,
            IsRead = n.IsRead
        }).ToList();

        return new CursorPagedResult<UserNotificationDto>
        {
            Items = dtos,
            NextCursor = nextCursor,
            HasMore = nextCursor.HasValue
        };
    }

    public Task<int> GetUnreadCountAsync(string userId) => _repo.GetUnreadCountAsync(userId);

    public async Task<UserNotificationDto> CreateNotificationAsync(NotificationEntity entity)
    {
        var created = await _repo.CreateAsync(entity);
        ActorDto? actor = null;
        if (created.ActorId != null && created.ActorType != "SYSTEM")
            actor = await _actorResolver.ResolveActorAsync(created.ActorId, created.ActorType);

        return new UserNotificationDto
        {
            Id = created.Id,
            UserId = created.UserId,
            Title = created.Title,
            Content = created.Content,
            Type = created.Type,
            ObjectId = created.ObjectId,
            Actor = actor,
            CreatedAt = created.CreatedAt,
            IsRead = created.IsRead
        };
    }

    public Task MarkAsReadAsync(int id, string userId) => _repo.MarkAsReadAsync(id, userId);
    public Task MarkAllAsReadAsync(string userId) => _repo.MarkAllAsReadAsync(userId);
}

using Notification.Application.DTOs;
using Notification.Application.Interfaces;
using Notification.Domain.Constants;
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
        return await BuildPagedResultAsync(items, nextCursor);
    }

    public async Task<CursorPagedResult<UserNotificationDto>> GetCommunityNotificationsAsync(string userId, int? cursor, int limit)
    {
        var (items, nextCursor) = await _repo.GetNotificationsByTypeFilterAsync(
            userId,
            cursor,
            limit,
            NotificationTypeGroups.CommunityTypes,
            includeTypes: true);
        return await BuildPagedResultAsync(items, nextCursor);
    }

    public async Task<CursorPagedResult<UserNotificationDto>> GetSystemNotificationsAsync(string userId, int? cursor, int limit)
    {
        var (items, nextCursor) = await _repo.GetNotificationsByTypeFilterAsync(
            userId,
            cursor,
            limit,
            NotificationTypeGroups.CommunityTypes,
            includeTypes: false);
        return await BuildPagedResultAsync(items, nextCursor);
    }

    private async Task<CursorPagedResult<UserNotificationDto>> BuildPagedResultAsync(List<NotificationEntity> items, int? nextCursor)
    {
        var actorMap = new Dictionary<string, ActorDto>();
        
        // First, try to use stored actor data from notifications
        var uniqueActors = items
            .Where(n => n.ActorId != null && n.ActorType != "SYSTEM")
            .Select(n => (n.ActorId!, n.ActorType, n.ActorName, n.ActorAvatar))
            .Distinct()
            .ToList();

        foreach (var (actorId, actorType, storedName, storedAvatar) in uniqueActors)
        {
            if (!actorMap.ContainsKey(actorId))
            {
                // Use stored actor data if available
                if (!string.IsNullOrWhiteSpace(storedName))
                {
                    actorMap[actorId] = new ActorDto
                    {
                        Id = int.TryParse(actorId, out var parsedId) ? parsedId : 0,
                        Name = storedName,
                        Avatar = storedAvatar ?? string.Empty,
                        Role = actorType == "COMPANY" ? "COMPANY" : "USER"
                    };
                }
                else
                {
                    // Fallback to HTTP enrichment if stored data missing
                    actorMap[actorId] = await ResolveActorAsync(actorId, actorType) ?? BuildFallbackActor(actorId);
                }
            }
        }

        var dtos = items.Select(n => new UserNotificationDto
        {
            Id = n.Id,
            UserId = n.UserId,
            Title = n.Title,
            Content = n.Content,
            Type = n.Type,
            ObjectId = n.ObjectId,
            Actor = n.ActorId != null ? actorMap.GetValueOrDefault(n.ActorId, BuildFallbackActor(n.ActorId)) : null,
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
        var actor = await ResolveActorAsync(created.ActorId, created.ActorType);
        return MapToUserDto(created, actor);
    }

    public async Task<NotificationCreatedEventDto> BuildCreatedEventAsync(NotificationEntity entity)
    {
        var actor = await ResolveActorAsync(entity.ActorId, entity.ActorType);
        return new NotificationCreatedEventDto
        {
            NotificationId = entity.Id,
            UserId = entity.UserId,
            Title = entity.Title,
            Content = entity.Content,
            Type = entity.Type,
            Category = NotificationTypeGroups.ResolveCategory(entity.Type),
            ObjectId = entity.ObjectId,
            Actor = actor,
            CreatedAt = entity.CreatedAt,
            IsRead = entity.IsRead
        };
    }

    public Task MarkAsReadAsync(int id, string userId) => _repo.MarkAsReadAsync(id, userId);
    public Task MarkAllAsReadAsync(string userId) => _repo.MarkAllAsReadAsync(userId);

    private async Task<ActorDto?> ResolveActorAsync(string? actorId, string actorType)
    {
        if (actorId is null || actorType == "SYSTEM")
        {
            return null;
        }

        return await _actorResolver.ResolveActorAsync(actorId, actorType) ?? BuildFallbackActor(actorId);
    }

    private static ActorDto BuildFallbackActor(string actorId)
    {
        return new ActorDto
        {
            Id = int.TryParse(actorId, out var parsedActorId) ? parsedActorId : 0,
            Name = "Unknown",
            Avatar = string.Empty,
            Role = "USER"
        };
    }

    private static UserNotificationDto MapToUserDto(NotificationEntity entity, ActorDto? actor)
    {
        return new UserNotificationDto
        {
            Id = entity.Id,
            UserId = entity.UserId,
            Title = entity.Title,
            Content = entity.Content,
            Type = entity.Type,
            ObjectId = entity.ObjectId,
            Actor = actor,
            CreatedAt = entity.CreatedAt,
            IsRead = entity.IsRead
        };
    }
}

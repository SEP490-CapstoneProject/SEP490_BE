using Microsoft.EntityFrameworkCore;
using Notification.Application.Interfaces;
using Notification.Domain.Entities;
using Notification.Infrastructure.Data;

namespace Notification.Infrastructure.Repositories;

public class NotificationRepository : INotificationRepository
{
    private readonly NotificationDbContext _db;

    public NotificationRepository(NotificationDbContext db) => _db = db;

    public async Task<(List<NotificationEntity> Items, int? NextCursor)> GetNotificationsAsync(string userId, int? cursor, int limit)
    {
        var query = _db.Notifications.Where(n => n.UserId == userId);

        if (cursor.HasValue)
            query = query.Where(n => n.Id < cursor.Value);

        var items = await query
            .OrderByDescending(n => n.Id)
            .Take(limit + 1)
            .ToListAsync();

        int? nextCursor = null;
        if (items.Count > limit)
        {
            nextCursor = items[limit - 1].Id;
            items = items.Take(limit).ToList();
        }

        return (items, nextCursor);
    }

    public Task<int> GetUnreadCountAsync(string userId) =>
        _db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);

    public async Task<NotificationEntity> CreateAsync(NotificationEntity entity)
    {
        _db.Notifications.Add(entity);
        await _db.SaveChangesAsync();
        return entity;
    }

    public async Task MarkAsReadAsync(int id, string userId)
    {
        var n = await _db.Notifications.FirstOrDefaultAsync(x => x.Id == id && x.UserId == userId);
        if (n is null) return;
        n.IsRead = true;
        await _db.SaveChangesAsync();
    }

    public async Task MarkAllAsReadAsync(string userId)
    {
        await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true));
    }
}

using Microsoft.EntityFrameworkCore;
using Subscription.Application.Interfaces;
using Subscription.Domain.Entities;
using Subscription.Infrastructure.Data;

namespace Subscription.Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly SubscriptionDbContext _context;

    public AuditLogService(SubscriptionDbContext context)
    {
        _context = context;
    }

    public async Task LogActionAsync(int adminUserId, string action, string entityType, int entityId, string? oldValues = null, string? newValues = null)
    {
        var log = new AdminAuditLog
        {
            AdminUserId = adminUserId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues,
            NewValues = newValues,
            CreatedAt = DateTime.UtcNow
        };

        _context.AdminAuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<AdminAuditLog>> GetLogsAsync(int? adminUserId = null, string? entityType = null, DateTime? from = null, DateTime? to = null)
    {
        var query = _context.AdminAuditLogs.AsQueryable();

        if (adminUserId.HasValue)
            query = query.Where(l => l.AdminUserId == adminUserId.Value);

        if (!string.IsNullOrEmpty(entityType))
            query = query.Where(l => l.EntityType == entityType);

        if (from.HasValue)
            query = query.Where(l => l.CreatedAt >= from.Value);

        if (to.HasValue)
            query = query.Where(l => l.CreatedAt <= to.Value);

        return await query.OrderByDescending(l => l.CreatedAt).ToListAsync();
    }
}

using Subscription.Domain.Entities;

namespace Subscription.Application.Interfaces;

public interface IAuditLogService
{
    Task LogActionAsync(int adminUserId, string action, string entityType, int entityId, string? oldValues = null, string? newValues = null);
    Task<IEnumerable<AdminAuditLog>> GetLogsAsync(int? adminUserId = null, string? entityType = null, DateTime? from = null, DateTime? to = null);
}

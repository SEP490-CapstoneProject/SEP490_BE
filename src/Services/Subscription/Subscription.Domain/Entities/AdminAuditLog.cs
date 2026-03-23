namespace Subscription.Domain.Entities;

public class AdminAuditLog
{
    public int Id { get; set; }
    public int AdminUserId { get; set; }
    public string Action { get; set; } = string.Empty;  // Create, Update, Delete, Cancel, Refund, Extend
    public string EntityType { get; set; } = string.Empty;  // Plan, Subscription, Feature
    public int EntityId { get; set; }
    public string? OldValues { get; set; }  // JSON serialized
    public string? NewValues { get; set; }  // JSON serialized
    public DateTime CreatedAt { get; set; }
}

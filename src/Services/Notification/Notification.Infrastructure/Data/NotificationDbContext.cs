using Microsoft.EntityFrameworkCore;
using Notification.Domain.Entities;

namespace Notification.Infrastructure.Data;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options) { }

    public DbSet<NotificationEntity> Notifications => Set<NotificationEntity>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.Entity<NotificationEntity>(e =>
        {
            e.ToTable("NOTIFICATION");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.UserId).HasMaxLength(50).IsRequired();
            e.Property(x => x.EventId).HasMaxLength(100);
            e.Property(x => x.Title).HasMaxLength(200).IsRequired();
            e.Property(x => x.Content).HasMaxLength(1000).IsRequired();
            e.Property(x => x.Type).HasMaxLength(50).IsRequired();
            e.Property(x => x.ObjectId).HasMaxLength(50);
            e.Property(x => x.ActorId).HasMaxLength(50);
            e.Property(x => x.ActorType).HasMaxLength(20).HasDefaultValue("SYSTEM");
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            e.Property(x => x.IsRead).HasDefaultValue(false);

            e.HasIndex(x => new { x.UserId, x.Id })
                .HasDatabaseName("IX_Notification_UserId_Id");
            e.HasIndex(x => new { x.UserId, x.IsRead })
                .HasDatabaseName("IX_Notification_UserId_IsRead");
            
            // Unique constraint to prevent duplicate notifications from the same event
            // Allows null EventId for backward compatibility with events that don't have EventId
            e.HasIndex(x => new { x.EventId, x.UserId, x.Type, x.ObjectId })
                .IsUnique()
                .HasDatabaseName("IX_Unique_Notification_Event")
                .HasFilter("[EventId] IS NOT NULL");
        });
    }
}

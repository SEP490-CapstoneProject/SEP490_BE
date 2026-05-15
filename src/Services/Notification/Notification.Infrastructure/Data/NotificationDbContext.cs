using Microsoft.EntityFrameworkCore;
using Notification.Domain.Entities;

namespace Notification.Infrastructure.Data;

public class NotificationDbContext : DbContext
{
    public NotificationDbContext(DbContextOptions<NotificationDbContext> options) : base(options) { }

    public DbSet<NotificationEntity> Notifications => Set<NotificationEntity>();
    public DbSet<DeviceTokenEntity> DeviceTokens => Set<DeviceTokenEntity>();
    public DbSet<PushNotificationLogEntity> PushNotificationLogs => Set<PushNotificationLogEntity>();
    public DbSet<NotificationSettingsEntity> NotificationSettings => Set<NotificationSettingsEntity>();

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
            e.Property(x => x.ActorName).HasMaxLength(255);
            e.Property(x => x.ActorAvatar).HasMaxLength(500);
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

        builder.Entity<DeviceTokenEntity>(e =>
        {
            e.ToTable("DEVICE_TOKENS");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.UserId).HasMaxLength(50).IsRequired();
            e.Property(x => x.DeviceToken).HasMaxLength(500).IsRequired();
            e.Property(x => x.DeviceType).HasMaxLength(20);
            e.Property(x => x.AppVersion).HasMaxLength(20);
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.Property(x => x.RegisteredAt).HasDefaultValueSql("GETUTCDATE()");

            e.HasIndex(x => x.DeviceToken).IsUnique();
            e.HasIndex(x => x.UserId).IsUnique();
        });

        builder.Entity<PushNotificationLogEntity>(e =>
        {
            e.ToTable("PUSH_NOTIFICATION_LOG");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.NotificationId).IsRequired();
            e.Property(x => x.DeviceTokenId).IsRequired();
            e.Property(x => x.Status).HasMaxLength(20);
            e.Property(x => x.MessageId).HasMaxLength(500);
            e.Property(x => x.ErrorCode).HasMaxLength(100);
            e.Property(x => x.ErrorMessage).HasMaxLength(500);
            e.Property(x => x.SentAt).HasDefaultValueSql("GETUTCDATE()");

            e.HasIndex(x => new { x.NotificationId, x.DeviceTokenId });
            e.HasIndex(x => x.Status);
        });

        builder.Entity<NotificationSettingsEntity>(e =>
        {
            e.ToTable("NOTIFICATION_SETTINGS");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).UseIdentityColumn();
            e.Property(x => x.UserId).HasMaxLength(50).IsRequired();
            e.Property(x => x.PushNotificationsEnabled).HasDefaultValue(true);
            e.Property(x => x.SoundEnabled).HasDefaultValue(true);
            e.Property(x => x.VibrateEnabled).HasDefaultValue(true);
            e.Property(x => x.ChatNotificationsEnabled).HasDefaultValue(true);
            e.Property(x => x.MentionNotificationsEnabled).HasDefaultValue(true);
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("GETUTCDATE()");

            e.HasIndex(x => x.UserId).IsUnique();
        });
    }
}

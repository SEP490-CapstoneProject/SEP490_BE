using Microsoft.EntityFrameworkCore;
using Payment.Domain.Entities;

namespace Payment.Infrastructure.Data;

public class PaymentDbContext : DbContext
{
    public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options)
    {
    }

    public DbSet<PaymentEntity> Payments => Set<PaymentEntity>();
    public DbSet<PaymentHistory> PaymentHistories => Set<PaymentHistory>();
    public DbSet<ProcessedEvent> ProcessedEvents => Set<ProcessedEvent>();
    public DbSet<OutboxEvent> OutboxEvents => Set<OutboxEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // PaymentEntity Configuration
        modelBuilder.Entity<PaymentEntity>(entity =>
        {
            entity.ToTable("Payments");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.UserId).IsRequired();
            entity.Property(e => e.PlanId).IsRequired();
            
            entity.Property(e => e.Amount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            entity.Property(e => e.Currency)
                .HasMaxLength(10)
                .IsRequired();

            entity.Property(e => e.Provider).IsRequired();
            entity.Property(e => e.Status).IsRequired();

            entity.Property(e => e.PaymentUrl)
                .HasMaxLength(1000);

            entity.Property(e => e.TransactionId)
                .HasMaxLength(100);

            entity.Property(e => e.OrderCode)
                .HasMaxLength(50)
                .IsRequired();

            // Optimistic concurrency token (manual increment strategy)
            entity.Property(e => e.RowVersion)
                .IsConcurrencyToken()
                .HasDefaultValue(0);

            entity.Property(e => e.Metadata)
                .HasMaxLength(2000);

            entity.Property(e => e.ExpiresAt);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt);
            entity.Property(e => e.PaidAt);

            // Indexes
            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("IX_Payments_UserId");

            entity.HasIndex(e => e.OrderCode)
                .IsUnique()
                .HasDatabaseName("IX_Payments_OrderCode");

            entity.HasIndex(e => new { e.Status, e.CreatedAt })
                .HasDatabaseName("IX_Payments_Status_CreatedAt");

            entity.HasIndex(e => new { e.UserId, e.PlanId, e.Status })
                .HasDatabaseName("IX_Payments_UserId_PlanId_Status");

            entity.HasIndex(e => new { e.Status, e.ExpiresAt })
                .HasDatabaseName("IX_Payments_Status_ExpiresAt");

            // Relationship with PaymentHistory
            entity.HasMany(e => e.History)
                .WithOne(h => h.Payment)
                .HasForeignKey(h => h.PaymentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // PaymentHistory Configuration
        modelBuilder.Entity<PaymentHistory>(entity =>
        {
            entity.ToTable("PaymentHistories");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.PaymentId).IsRequired();

            entity.Property(e => e.OldStatus)
                .HasMaxLength(50);

            entity.Property(e => e.NewStatus)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Action)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.RawData)
                .HasMaxLength(4000);

            entity.Property(e => e.CorrelationId)
                .HasMaxLength(100);

            entity.Property(e => e.CreatedAt).IsRequired();

            // Index
            entity.HasIndex(e => e.PaymentId)
                .HasDatabaseName("IX_PaymentHistories_PaymentId");
        });

        // ProcessedEvent Configuration
        modelBuilder.Entity<ProcessedEvent>(entity =>
        {
            entity.ToTable("ProcessedEvents");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.EventId)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.EventType)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.EventHash)
                .HasMaxLength(64)
                .IsRequired();

            entity.Property(e => e.OrderCode)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.ProcessedAt).IsRequired();

            entity.Property(e => e.CorrelationId)
                .HasMaxLength(100);

            // ✅ UNIQUE INDEX on EventHash (primary idempotency check)
            entity.HasIndex(e => e.EventHash)
                .IsUnique()
                .HasDatabaseName("UX_ProcessedEvents_EventHash");

            // ✅ UNIQUE INDEX on OrderCode (secondary idempotency check)
            entity.HasIndex(e => e.OrderCode)
                .IsUnique()
                .HasDatabaseName("UX_ProcessedEvents_OrderCode");

            // Index for cleanup queries
            entity.HasIndex(e => e.ProcessedAt)
                .HasDatabaseName("IX_ProcessedEvents_ProcessedAt");
        });

        // OutboxEvent Configuration
        modelBuilder.Entity<OutboxEvent>(entity =>
        {
            entity.ToTable("OutboxEvents");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.EventId)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.EventType)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.Payload)
                .HasMaxLength(4000)
                .IsRequired();

            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.RetryCount).IsRequired();
            entity.Property(e => e.NextRetryAt);

            entity.Property(e => e.LastError)
                .HasMaxLength(2000);

            entity.Property(e => e.CreatedAt).IsRequired();

            // Index
            entity.HasIndex(e => new { e.Status, e.NextRetryAt })
                .HasDatabaseName("IX_OutboxEvents_Status_NextRetryAt");
        });
    }
}

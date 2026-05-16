using Microsoft.EntityFrameworkCore;
using Subscription.Domain.Entities;

namespace Subscription.Infrastructure.Data;

public class SubscriptionDbContext : DbContext
{
    public SubscriptionDbContext(DbContextOptions<SubscriptionDbContext> options) : base(options)
    {
    }

    public DbSet<Plan> Plans { get; set; }
    public DbSet<PlanFeature> PlanFeatures { get; set; }
    public DbSet<UserSubscription> Subscriptions { get; set; }
    public DbSet<ProcessedEvent> ProcessedEvents { get; set; }
    public DbSet<OutboxEvent> OutboxEvents { get; set; }
    public DbSet<AdminAuditLog> AdminAuditLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Plan configuration
        modelBuilder.Entity<Plan>(entity =>
        {
            entity.ToTable("Plans");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
            entity.Property(e => e.AllowedRole).HasMaxLength(50);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            
            entity.HasMany(e => e.Features)
                .WithOne(f => f.Plan)
                .HasForeignKey(f => f.PlanId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasMany(e => e.Subscriptions)
                .WithOne(s => s.Plan)
                .HasForeignKey(s => s.PlanId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // PlanFeature configuration
        modelBuilder.Entity<PlanFeature>(entity =>
        {
            entity.ToTable("PlanFeatures");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.FeatureKey).IsRequired().HasMaxLength(50);
            entity.Property(e => e.FeatureName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Value).IsRequired().HasMaxLength(200);
            
            entity.HasIndex(e => new { e.PlanId, e.FeatureKey }).IsUnique();
        });

        // UserSubscription configuration
        modelBuilder.Entity<UserSubscription>(entity =>
        {
            entity.ToTable("Subscriptions");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.PlanId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.UserId, e.Status });
        });

        // ProcessedEvent configuration
        modelBuilder.Entity<ProcessedEvent>(entity =>
        {
            entity.ToTable("ProcessedEvents");
            entity.HasKey(e => e.EventId);
            entity.Property(e => e.EventId).HasMaxLength(100);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.ProcessedAt).HasDefaultValueSql("GETDATE()");
            
            entity.HasIndex(e => e.ProcessedAt);
        });

        // OutboxEvent configuration
        modelBuilder.Entity<OutboxEvent>(entity =>
        {
            entity.ToTable("OutboxEvents");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EventId).IsRequired().HasMaxLength(100);
            entity.Property(e => e.EventType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Payload).IsRequired();
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.ErrorMessage).HasMaxLength(1000);
            
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => new { e.Status, e.CreatedAt });
        });

        // AdminAuditLog configuration
        modelBuilder.Entity<AdminAuditLog>(entity =>
        {
            entity.ToTable("AdminAuditLogs");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Action).IsRequired().HasMaxLength(50);
            entity.Property(e => e.EntityType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.HasIndex(e => new { e.AdminUserId, e.CreatedAt });
            entity.HasIndex(e => new { e.EntityType, e.EntityId });
        });

        // Seed Plans
        SeedPlans(modelBuilder);
    }

    private static void SeedPlans(ModelBuilder modelBuilder)
    {
        // Seed Plans
        modelBuilder.Entity<Plan>().HasData(
            new Plan { Id = 1, Name = "Free", Description = "Basic free plan", Price = 0, BillingCycle = Domain.Enums.BillingCycle.Monthly, IsActive = true, CreatedAt = new DateTime(2026, 1, 1) },
            new Plan { Id = 2, Name = "Pro", Description = "Professional plan with advanced features", Price = 9.99m, BillingCycle = Domain.Enums.BillingCycle.Monthly, IsActive = true, CreatedAt = new DateTime(2026, 1, 1) },
            new Plan { Id = 3, Name = "Premium", Description = "Premium plan with unlimited access", Price = 19.99m, BillingCycle = Domain.Enums.BillingCycle.Monthly, IsActive = true, CreatedAt = new DateTime(2026, 1, 1) }
        );

        // Seed Free Plan Features
        modelBuilder.Entity<PlanFeature>().HasData(
            new PlanFeature { Id = 1, PlanId = 1, FeatureKey = "MAX_APPLY", FeatureName = "Max Applications", Value = "5", Type = Domain.Enums.FeatureType.Number, IsActive = true },
            new PlanFeature { Id = 2, PlanId = 1, FeatureKey = "MAX_PORTFOLIOS", FeatureName = "Max Portfolios", Value = "1", Type = Domain.Enums.FeatureType.Number, IsActive = true },
            new PlanFeature { Id = 3, PlanId = 1, FeatureKey = "AI_MATCHING", FeatureName = "AI Matching", Value = "false", Type = Domain.Enums.FeatureType.Boolean, IsActive = true },
            new PlanFeature { Id = 4, PlanId = 1, FeatureKey = "BOOST_PROFILE", FeatureName = "Profile Boost", Value = "false", Type = Domain.Enums.FeatureType.Boolean, IsActive = true },
            new PlanFeature { Id = 5, PlanId = 1, FeatureKey = "COMPLIMENT_ACCESS", FeatureName = "Compliment Access", Value = "false", Type = Domain.Enums.FeatureType.Boolean, IsActive = true }
        );

        // Seed Pro Plan Features
        modelBuilder.Entity<PlanFeature>().HasData(
            new PlanFeature { Id = 6, PlanId = 2, FeatureKey = "MAX_APPLY", FeatureName = "Max Applications", Value = "20", Type = Domain.Enums.FeatureType.Number, IsActive = true },
            new PlanFeature { Id = 7, PlanId = 2, FeatureKey = "MAX_PORTFOLIOS", FeatureName = "Max Portfolios", Value = "5", Type = Domain.Enums.FeatureType.Number, IsActive = true },
            new PlanFeature { Id = 8, PlanId = 2, FeatureKey = "AI_MATCHING", FeatureName = "AI Matching", Value = "true", Type = Domain.Enums.FeatureType.Boolean, IsActive = true },
            new PlanFeature { Id = 9, PlanId = 2, FeatureKey = "BOOST_PROFILE", FeatureName = "Profile Boost", Value = "true", Type = Domain.Enums.FeatureType.Boolean, IsActive = true },
            new PlanFeature { Id = 10, PlanId = 2, FeatureKey = "COMPLIMENT_ACCESS", FeatureName = "Compliment Access", Value = "false", Type = Domain.Enums.FeatureType.Boolean, IsActive = true }
        );

        // Seed Premium Plan Features
        modelBuilder.Entity<PlanFeature>().HasData(
            new PlanFeature { Id = 11, PlanId = 3, FeatureKey = "MAX_APPLY", FeatureName = "Max Applications", Value = "-1", Type = Domain.Enums.FeatureType.Number, IsActive = true },
            new PlanFeature { Id = 12, PlanId = 3, FeatureKey = "MAX_PORTFOLIOS", FeatureName = "Max Portfolios", Value = "-1", Type = Domain.Enums.FeatureType.Number, IsActive = true },
            new PlanFeature { Id = 13, PlanId = 3, FeatureKey = "AI_MATCHING", FeatureName = "AI Matching", Value = "true", Type = Domain.Enums.FeatureType.Boolean, IsActive = true },
            new PlanFeature { Id = 14, PlanId = 3, FeatureKey = "BOOST_PROFILE", FeatureName = "Profile Boost", Value = "true", Type = Domain.Enums.FeatureType.Boolean, IsActive = true },
            new PlanFeature { Id = 15, PlanId = 3, FeatureKey = "COMPLIMENT_ACCESS", FeatureName = "Compliment Access", Value = "true", Type = Domain.Enums.FeatureType.Boolean, IsActive = true }
        );
    }
}

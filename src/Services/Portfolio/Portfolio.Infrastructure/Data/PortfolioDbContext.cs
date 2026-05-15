using Microsoft.EntityFrameworkCore;
using Portfolio.Application.Interfaces;
using Portfolio.Domain.Entities;
using Portfolio.Domain.Enums;

namespace Portfolio.Infrastructure.Data;

public class PortfolioDbContext : DbContext
{
    private readonly ICurrentUserService _currentUser;

    public PortfolioDbContext(DbContextOptions<PortfolioDbContext> options, ICurrentUserService currentUser)
        : base(options)
    {
        _currentUser = currentUser;
    }

    public DbSet<Portfolio.Domain.Entities.Portfolio> Portfolios { get; set; }
    public DbSet<BlockType> BlockTypes { get; set; }
    public DbSet<PortfolioBlock> PortfolioBlocks { get; set; }
    public DbSet<Compliment> Compliments { get; set; }
    public DbSet<PortfolioFollow> PortfolioFollows { get; set; }
    public DbSet<PortfolioFollowCategory> PortfolioFollowCategories { get; set; }
    public DbSet<Criterion> Criteria { get; set; }
    public DbSet<PortfolioPreview> PortfolioPreview { get; set; }
    public DbSet<PortfolioReport> PortfolioReports { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Portfolio
        modelBuilder.Entity<Portfolio.Domain.Entities.Portfolio>(e =>
        {
            e.ToTable("Portfolio");
            e.HasKey(x => x.Id);
            e.Property(x => x.EmployeeId).IsRequired();
            e.Property(x => x.Name).HasMaxLength(255).IsRequired();
            e.Property(x => x.Status).HasMaxLength(50).HasDefaultValue("active");
            e.Property(x => x.IsMain).HasDefaultValue(false);
            e.Property(x => x.IsPublic).HasDefaultValue(false);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
            e.Property(x => x.UpdatedAt).IsRequired(false);
            e.Property(x => x.ComplimentCount).HasDefaultValue(0);
            e.Property(x => x.ApprovedComplimentCount).HasDefaultValue(0);
            e.Property(x => x.AverageScore).HasColumnType("decimal(3,2)").IsRequired(false);
            e.Property(x => x.Embedding).HasColumnType("nvarchar(max)").IsRequired(false);
            e.Property(x => x.EmbeddingVersion).HasDefaultValue(0);
            e.Property(x => x.EmbeddingUpdatedAt).IsRequired(false);
            e.Property(x => x.EmbeddingStatus).HasMaxLength(20).HasDefaultValue("Pending");
            e.Property(x => x.ModerationStatus).HasMaxLength(20).HasDefaultValue("PendingReview");
            e.Property(x => x.ModerationReason).HasMaxLength(500).IsRequired(false);
            e.Property(x => x.ModeratedAt).IsRequired(false);
            e.HasIndex(x => x.EmployeeId).HasDatabaseName("IX_Portfolio_EmployeeId");
            e.HasIndex(x => new { x.EmployeeId, x.IsMain })
                .HasDatabaseName("UX_Portfolio_EmployeeId_IsMain")
                .IsUnique()
                .HasFilter("[IsMain] = 1");
        });

        // BlockType
        modelBuilder.Entity<BlockType>(e =>
        {
            e.ToTable("BlockType");
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(100).IsRequired();
            e.HasIndex(x => x.Code).IsUnique();
            e.Property(x => x.IsMultiple).HasDefaultValue(true);
            e.Property(x => x.IsActive).HasDefaultValue(true);

            e.HasData(
                new BlockType { Id = 1, Code = "INTRO", IsMultiple = false, IsActive = true },
                new BlockType { Id = 2, Code = "SKILL", IsMultiple = true, IsActive = true },
                new BlockType { Id = 3, Code = "EDUCATION", IsMultiple = true, IsActive = true },
                new BlockType { Id = 4, Code = "DIPLOMA", IsMultiple = true, IsActive = true },
                new BlockType { Id = 5, Code = "EXPERIMENT", IsMultiple = true, IsActive = true },
                new BlockType { Id = 6, Code = "PROJECT", IsMultiple = true, IsActive = true },
                new BlockType { Id = 7, Code = "AWARD", IsMultiple = true, IsActive = true },
                new BlockType { Id = 8, Code = "ACTIVITIES", IsMultiple = true, IsActive = true },
                new BlockType { Id = 9, Code = "OTHERINFO", IsMultiple = true, IsActive = true },
                new BlockType { Id = 10, Code = "REFERENCE", IsMultiple = true, IsActive = true }
            );
        });

        // PortfolioBlock
        modelBuilder.Entity<PortfolioBlock>(e =>
        {
            e.ToTable("PortfolioBlock");
            e.HasKey(x => x.Id);
            e.Property(x => x.Variant).HasMaxLength(100).IsRequired();
            e.Property(x => x.IsVisible).HasDefaultValue(true);
            e.Property(x => x.DataJson).HasColumnType("nvarchar(max)").HasDefaultValue("{}");

            e.HasIndex(x => new { x.PortfolioId, x.BlockTypeId, x.DisplayOrder })
             .HasDatabaseName("IX_Block_Portfolio_Type_Order");

            e.HasOne(x => x.Portfolio)
             .WithMany(x => x.Blocks)
             .HasForeignKey(x => x.PortfolioId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.BlockType)
             .WithMany(x => x.PortfolioBlocks)
             .HasForeignKey(x => x.BlockTypeId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Compliment
        modelBuilder.Entity<Compliment>(e =>
        {
            e.ToTable("Compliment");
            e.HasKey(x => x.Id);
            e.Property(x => x.Content).HasColumnType("nvarchar(max)").IsRequired(false);
            e.Property(x => x.State).HasDefaultValue(ComplimentState.Pending);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
            e.Property(x => x.Score).IsRequired(false);
            e.Property(x => x.UpdatedAt).IsRequired(false);
            e.Property(x => x.UpdatedBy).IsRequired(false);

            // Filtered UNIQUE: one active compliment per user per portfolio
            e.HasIndex(x => new { x.PortfolioId, x.UserId })
             .HasDatabaseName("UX_Compliment_Portfolio_User_Active")
             .HasFilter("[State] <> 3")
             .IsUnique();

            e.HasIndex(x => new { x.PortfolioId, x.UserId, x.State })
             .HasDatabaseName("IX_Compliment_Portfolio_User_State");

            // Global query filter: multi-tenant isolation + state-based deletion
            e.HasQueryFilter(c =>
                c.State != ComplimentState.Deleted &&
                (_currentUser.IsAdmin ||
                 (_currentUser.UserId > 0 && c.UserId == _currentUser.UserId)));

            e.HasOne(x => x.Portfolio)
             .WithMany(x => x.Compliments)
             .HasForeignKey(x => x.PortfolioId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // PortfolioFollow
        modelBuilder.Entity<PortfolioFollow>(e =>
        {
            e.ToTable("PortfolioFollow");
            e.HasKey(x => x.Id);
            e.Property(x => x.CompanyId).IsRequired();
            e.Property(x => x.PortfolioId).IsRequired();
            e.Property(x => x.CategoryId).IsRequired(false);
            e.Property(x => x.InterestLevel).HasMaxLength(10).IsRequired();
            e.Property(x => x.FollowedAt).HasDefaultValueSql("DATEADD(HOUR, 7, GETUTCDATE())");
            e.Property(x => x.UpdatedAt).IsRequired(false);

            e.HasIndex(x => x.CompanyId).HasDatabaseName("IX_PortfolioFollow_CompanyId");
            e.HasIndex(x => x.PortfolioId).HasDatabaseName("IX_PortfolioFollow_PortfolioId");
            e.HasIndex(x => x.CategoryId).HasDatabaseName("IX_PortfolioFollow_CategoryId");
            e.HasIndex(x => new { x.CompanyId, x.PortfolioId })
             .HasDatabaseName("UX_PortfolioFollow_Company_Portfolio")
             .IsUnique();

            e.HasOne(x => x.Portfolio)
             .WithMany(x => x.Follows)
             .HasForeignKey(x => x.PortfolioId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.Category)
             .WithMany(x => x.Follows)
             .HasForeignKey(x => x.CategoryId)
             .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<PortfolioFollowCategory>(e =>
        {
            e.ToTable("PortfolioFollowCategory");
            e.HasKey(x => x.Id);
            e.Property(x => x.CompanyId).IsRequired();
            e.Property(x => x.Name).HasMaxLength(100).IsRequired();
            e.Property(x => x.Code).HasMaxLength(100).IsRequired();
            e.Property(x => x.CreatedAt).HasDefaultValueSql("DATEADD(HOUR, 7, GETUTCDATE())");
            e.Property(x => x.UpdatedAt).IsRequired(false);

            e.HasIndex(x => x.CompanyId).HasDatabaseName("IX_PortfolioFollowCategory_CompanyId");
            e.HasIndex(x => new { x.CompanyId, x.Code })
             .HasDatabaseName("UX_PortfolioFollowCategory_Company_Code")
             .IsUnique();
        });

        // Criterion
        modelBuilder.Entity<Criterion>(e =>
        {
            e.ToTable("Criterion");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(255).IsRequired();
            e.Property(x => x.Kind).HasMaxLength(100).IsRequired();
            e.Property(x => x.IsActive).HasDefaultValue(true);

            e.HasIndex(x => x.Kind).HasDatabaseName("IX_Criterion_Kind");
        });

        // PortfolioPreview
        modelBuilder.Entity<PortfolioPreview>(e =>
        {
            e.ToTable("PortfolioPreview");
            e.HasKey(x => x.Id);
            e.Property(x => x.PortfolioId).IsRequired();
            e.Property(x => x.PreviewJson).HasColumnType("nvarchar(max)").HasDefaultValue("{}");
            e.Property(x => x.HighlightsDescription).HasMaxLength(500).IsRequired(false);
            e.Property(x => x.Version).HasDefaultValue(1);
            e.Property(x => x.RegeneratedCount).HasDefaultValue(0);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
            e.Property(x => x.UpdatedAt).HasDefaultValueSql("GETDATE()");
            e.Property(x => x.IsActive).HasDefaultValue(true);
            e.Property(x => x.GenerationModel).HasMaxLength(50).HasDefaultValue("gemini-1.5-pro");
            e.Property(x => x.TokensUsed).IsRequired(false);

            e.HasIndex(x => x.PortfolioId)
                .IsUnique()
                .HasDatabaseName("UX_PortfolioPreview_PortfolioId");

            e.HasOne(x => x.Portfolio)
                .WithMany()
                .HasForeignKey(x => x.PortfolioId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // PortfolioReport
        modelBuilder.Entity<PortfolioReport>(e =>
        {
            e.ToTable("PortfolioReport");
            e.HasKey(x => x.Id);
            e.Property(x => x.PortfolioId).IsRequired();
            e.Property(x => x.ReporterUserId).IsRequired();
            e.Property(x => x.Reason).HasMaxLength(100).IsRequired();
            e.Property(x => x.Description).HasMaxLength(1000).IsRequired(false);
            e.Property(x => x.Status).HasConversion<int>().HasDefaultValue(PortfolioReportStatus.Pending);
            e.Property(x => x.ReviewedByUserId).IsRequired(false);
            e.Property(x => x.ReviewedAt).IsRequired(false);
            e.Property(x => x.ReviewNote).HasMaxLength(1000).IsRequired(false);
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
            e.Property(x => x.UpdatedAt).IsRequired(false);

            e.HasIndex(x => new { x.PortfolioId, x.ReporterUserId })
                .IsUnique()
                .HasDatabaseName("UX_PortfolioReport_Portfolio_Reporter");
            e.HasIndex(x => x.Status).HasDatabaseName("IX_PortfolioReport_Status");
            e.HasIndex(x => x.PortfolioId).HasDatabaseName("IX_PortfolioReport_PortfolioId");

            e.HasOne(x => x.Portfolio)
                .WithMany()
                .HasForeignKey(x => x.PortfolioId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

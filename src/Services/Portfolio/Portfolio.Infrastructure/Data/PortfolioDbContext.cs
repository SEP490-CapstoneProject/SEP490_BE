using Microsoft.EntityFrameworkCore;
using Portfolio.Domain.Entities;

namespace Portfolio.Infrastructure.Data;

public class PortfolioDbContext : DbContext
{
    public PortfolioDbContext(DbContextOptions<PortfolioDbContext> options) : base(options) { }

    public DbSet<Portfolio.Domain.Entities.Portfolio> Portfolios { get; set; }
    public DbSet<BlockType> BlockTypes { get; set; }
    public DbSet<PortfolioBlock> PortfolioBlocks { get; set; }

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
            e.Property(x => x.CreatedAt).HasDefaultValueSql("GETDATE()");
            e.Property(x => x.UpdatedAt).IsRequired(false);
            e.HasIndex(x => x.EmployeeId).HasDatabaseName("IX_Portfolio_EmployeeId");
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

            // Composite index: load all blocks of a specific type within a portfolio, in order
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
    }
}

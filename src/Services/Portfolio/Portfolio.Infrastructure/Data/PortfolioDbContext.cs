using Microsoft.EntityFrameworkCore;
using Portfolio.Domain.Entities;

namespace Portfolio.Infrastructure.Data;

public class PortfolioDbContext : DbContext
{
    public PortfolioDbContext(DbContextOptions<PortfolioDbContext> options) : base(options) { }

    public DbSet<Portfolio.Domain.Entities.Portfolio> Portfolios { get; set; }
    public DbSet<BlockType> BlockTypes { get; set; }
    public DbSet<PortfolioBlock> PortfolioBlocks { get; set; }
    public DbSet<Intro> Intros { get; set; }
    public DbSet<Skill> Skills { get; set; }
    public DbSet<Education> Educations { get; set; }
    public DbSet<Diploma> Diplomas { get; set; }
    public DbSet<Experience> Experiences { get; set; }
    public DbSet<Project> Projects { get; set; }
    public DbSet<ProjectLink> ProjectLinks { get; set; }
    public DbSet<Award> Awards { get; set; }
    public DbSet<Activities> Activities { get; set; }
    public DbSet<OtherInfo> OtherInfos { get; set; }
    public DbSet<Reference> References { get; set; }

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

            // Seed block types
            e.HasData(
                new BlockType { Id = 1, Code = "INTRO", IsMultiple = false },
                new BlockType { Id = 2, Code = "SKILL", IsMultiple = true },
                new BlockType { Id = 3, Code = "EDUCATION", IsMultiple = true },
                new BlockType { Id = 4, Code = "DIPLOMA", IsMultiple = true },
                new BlockType { Id = 5, Code = "EXPERIMENT", IsMultiple = true },
                new BlockType { Id = 6, Code = "PROJECT", IsMultiple = true },
                new BlockType { Id = 7, Code = "AWARD", IsMultiple = true },
                new BlockType { Id = 8, Code = "ACTIVITIES", IsMultiple = true },
                new BlockType { Id = 9, Code = "OTHERINFO", IsMultiple = true },
                new BlockType { Id = 10, Code = "REFERENCE", IsMultiple = true }
            );
        });

        // PortfolioBlock
        modelBuilder.Entity<PortfolioBlock>(e =>
        {
            e.ToTable("PortfolioBlock");
            e.HasKey(x => x.Id);
            e.Property(x => x.Variant).HasMaxLength(100).IsRequired();
            e.Property(x => x.IsVisible).HasDefaultValue(true);
            e.HasIndex(x => x.PortfolioId).HasDatabaseName("IX_PortfolioBlock_PortfolioId");

            e.HasOne(x => x.Portfolio)
             .WithMany(x => x.Blocks)
             .HasForeignKey(x => x.PortfolioId)
             .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(x => x.BlockType)
             .WithMany(x => x.PortfolioBlocks)
             .HasForeignKey(x => x.BlockTypeId)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // Intro (1-1)
        modelBuilder.Entity<Intro>(e =>
        {
            e.ToTable("Intro");
            e.HasKey(x => x.Id);
            e.Property(x => x.Avatar).HasMaxLength(500);
            e.Property(x => x.Name).HasMaxLength(255);
            e.Property(x => x.StudyField).HasMaxLength(255);
            e.Property(x => x.Email).HasMaxLength(255);
            e.Property(x => x.Phone).HasMaxLength(50);
            e.HasIndex(x => x.PortfolioBlockId).IsUnique();

            e.HasOne(x => x.PortfolioBlock)
             .WithOne(x => x.Intro)
             .HasForeignKey<Intro>(x => x.PortfolioBlockId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Skill
        modelBuilder.Entity<Skill>(e =>
        {
            e.ToTable("Skill");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(255).IsRequired();

            e.HasOne(x => x.PortfolioBlock)
             .WithMany(x => x.Skills)
             .HasForeignKey(x => x.PortfolioBlockId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Education
        modelBuilder.Entity<Education>(e =>
        {
            e.ToTable("Education");
            e.HasKey(x => x.Id);
            e.Property(x => x.SchoolName).HasMaxLength(255);
            e.Property(x => x.Time).HasMaxLength(100);
            e.Property(x => x.Department).HasMaxLength(255);

            e.HasOne(x => x.PortfolioBlock)
             .WithMany(x => x.Educations)
             .HasForeignKey(x => x.PortfolioBlockId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Diploma
        modelBuilder.Entity<Diploma>(e =>
        {
            e.ToTable("Diploma");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(255);
            e.Property(x => x.Provider).HasMaxLength(255);
            e.Property(x => x.Link).HasMaxLength(500);

            e.HasOne(x => x.PortfolioBlock)
             .WithMany(x => x.Diplomas)
             .HasForeignKey(x => x.PortfolioBlockId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Experience
        modelBuilder.Entity<Experience>(e =>
        {
            e.ToTable("Experience");
            e.HasKey(x => x.Id);
            e.Property(x => x.JobName).HasMaxLength(255);
            e.Property(x => x.Address).HasMaxLength(255);
            e.Property(x => x.StartDate).HasMaxLength(50);
            e.Property(x => x.EndDate).HasMaxLength(50);

            e.HasOne(x => x.PortfolioBlock)
             .WithMany(x => x.Experiences)
             .HasForeignKey(x => x.PortfolioBlockId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Project
        modelBuilder.Entity<Project>(e =>
        {
            e.ToTable("Project");
            e.HasKey(x => x.Id);
            e.Property(x => x.Image).HasMaxLength(500);
            e.Property(x => x.Name).HasMaxLength(255);
            e.Property(x => x.Role).HasMaxLength(255);
            e.Property(x => x.Technology).HasMaxLength(255);

            e.HasOne(x => x.PortfolioBlock)
             .WithMany(x => x.Projects)
             .HasForeignKey(x => x.PortfolioBlockId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ProjectLink
        modelBuilder.Entity<ProjectLink>(e =>
        {
            e.ToTable("ProjectLink");
            e.HasKey(x => x.Id);
            e.Property(x => x.Type).HasMaxLength(100);
            e.Property(x => x.Link).HasMaxLength(500);

            e.HasOne(x => x.Project)
             .WithMany(x => x.Links)
             .HasForeignKey(x => x.ProjectId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Award
        modelBuilder.Entity<Award>(e =>
        {
            e.ToTable("Award");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(255);
            e.Property(x => x.Organization).HasMaxLength(255);

            e.HasOne(x => x.PortfolioBlock)
             .WithMany(x => x.Awards)
             .HasForeignKey(x => x.PortfolioBlockId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Activities
        modelBuilder.Entity<Activities>(e =>
        {
            e.ToTable("Activities");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(255);

            e.HasOne(x => x.PortfolioBlock)
             .WithMany(x => x.Activities)
             .HasForeignKey(x => x.PortfolioBlockId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // OtherInfo
        modelBuilder.Entity<OtherInfo>(e =>
        {
            e.ToTable("OtherInfo");
            e.HasKey(x => x.Id);
            e.Property(x => x.Detail).HasMaxLength(500);

            e.HasOne(x => x.PortfolioBlock)
             .WithMany(x => x.OtherInfos)
             .HasForeignKey(x => x.PortfolioBlockId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Reference
        modelBuilder.Entity<Reference>(e =>
        {
            e.ToTable("Reference");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(255);
            e.Property(x => x.Position).HasMaxLength(255);
            e.Property(x => x.Mail).HasMaxLength(255);
            e.Property(x => x.Phone).HasMaxLength(50);

            e.HasOne(x => x.PortfolioBlock)
             .WithMany(x => x.References)
             .HasForeignKey(x => x.PortfolioBlockId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}

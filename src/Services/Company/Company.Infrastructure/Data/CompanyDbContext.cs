using Company.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Company.Infrastructure.Data;

public class CompanyDbContext : DbContext
{
    public CompanyDbContext(DbContextOptions<CompanyDbContext> options) : base(options) { }

    public DbSet<CompanyPost> CompanyPosts { get; set; }
    public DbSet<CompanyPostMedia> CompanyPostMedia { get; set; }
    public DbSet<CompanyPostSave> CompanyPostSaves { get; set; }
    public DbSet<CompanyEntity> Companies { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("companysvc");

        modelBuilder.Entity<CompanyPost>(entity =>
        {
            entity.ToTable("COMPANY_POST", "companysvc");
            entity.HasKey(e => e.PostId);
            entity.Property(e => e.PostId).HasColumnName("postId");
            entity.Property(e => e.CompanyId).HasColumnName("companyId").IsRequired();
            entity.Property(e => e.Position).HasColumnName("position").HasMaxLength(255).IsRequired();
            entity.Property(e => e.Address).HasColumnName("address").HasMaxLength(500);
            entity.Property(e => e.Salary).HasColumnName("salary").HasMaxLength(255);
            entity.Property(e => e.EmploymentType).HasColumnName("employmentType").HasMaxLength(50);
            entity.Property(e => e.ExperienceYear).HasColumnName("experienceYear");
            entity.Property(e => e.Quantity).HasColumnName("quantity");
            entity.Property(e => e.JobDescription).HasColumnName("jobDescription");
            entity.Property(e => e.RequirementsMandatory).HasColumnName("requirementsMandatory");
            entity.Property(e => e.RequirementsPreferred).HasColumnName("requirementsPreferred");
            entity.Property(e => e.Benefits).HasColumnName("benefits");
            entity.Property(e => e.CoverImageVideo).HasColumnName("coverImageVideo").HasMaxLength(500);
            entity.Property(e => e.CreatedAt).HasColumnName("createAt").HasDefaultValueSql("GETDATE()");
            entity.Property(e => e.Status).HasColumnName("status").HasDefaultValue(1);

            entity.HasOne(e => e.Company)
                .WithMany(c => c.Posts)
                .HasForeignKey(e => e.CompanyId)
                .HasConstraintName("FK_CompanyPost_Company")
                .OnDelete(DeleteBehavior.Cascade);

            // Feed sort index: status + createAt DESC + postId DESC
            entity.HasIndex(e => new { e.Status, e.CreatedAt, e.PostId })
                .HasDatabaseName("IX_Post_Status_CreateAt_PostId");

            // Company profile page index
            entity.HasIndex(e => e.CompanyId)
                .HasDatabaseName("IX_Post_CompanyId");
        });

        modelBuilder.Entity<CompanyPostMedia>(entity =>
        {
            entity.ToTable("COMPANY_POST_MEDIA", "companysvc");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CompanyPostId).HasColumnName("companyPostId").IsRequired();
            entity.Property(e => e.Type).HasColumnName("type").HasMaxLength(50);
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(255);
            entity.Property(e => e.Address).HasColumnName("address").HasMaxLength(500);

            entity.HasOne(e => e.CompanyPost)
                .WithMany(p => p.Media)
                .HasForeignKey(e => e.CompanyPostId)
                .HasConstraintName("FK_PostMedia_Post")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.CompanyPostId)
                .HasDatabaseName("IX_PostMedia_PostId");
        });

        modelBuilder.Entity<CompanyPostSave>(entity =>
        {
            entity.ToTable("COMPANY_POST_SAVE", "companysvc");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CompanyPostId).HasColumnName("companyPostId").IsRequired();
            entity.Property(e => e.UserId).HasColumnName("userId").IsRequired();

            entity.HasOne(e => e.CompanyPost)
                .WithMany(p => p.Saves)
                .HasForeignKey(e => e.CompanyPostId)
                .HasConstraintName("FK_PostSave_Post")
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.UserId, e.CompanyPostId })
                .IsUnique()
                .HasDatabaseName("IX_PostSave_User_Post");
        });

        modelBuilder.Entity<CompanyEntity>(entity =>
        {
            entity.ToTable("COMPANY", "companysvc");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(255);
            entity.Property(e => e.AvatarUrl).HasColumnName("avatarUrl").HasMaxLength(500);
        });
    }
}

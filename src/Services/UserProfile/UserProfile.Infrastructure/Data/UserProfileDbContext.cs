using Microsoft.EntityFrameworkCore;
using UserProfile.Domain.Entities;

namespace UserProfile.Infrastructure.Data;

public class UserProfileDbContext : DbContext
{
    public UserProfileDbContext(DbContextOptions<UserProfileDbContext> options) : base(options)
    {
    }

    public DbSet<Employee> Employees { get; set; }
    public DbSet<Company> Companies { get; set; }
    public DbSet<Expert> Experts { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Employee configuration
        modelBuilder.Entity<Employee>(entity =>
        {
            entity.ToTable("employee");
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.UserId).HasColumnName("userId").IsRequired();
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
            entity.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(50);
            entity.Property(e => e.CoverImage).HasColumnName("coverImage").HasMaxLength(500);
            entity.Property(e => e.Avatar).HasColumnName("avatar").HasMaxLength(500);

            // Unique index on userId
            entity.HasIndex(e => e.UserId).IsUnique().HasDatabaseName("IX_employee_userId");
        });

        // Company configuration
        modelBuilder.Entity<Company>(entity =>
        {
            entity.ToTable("company");
            entity.HasKey(c => c.Id);
            
            entity.Property(c => c.Id).HasColumnName("companyId");
            entity.Property(c => c.UserId).HasColumnName("userId").IsRequired();
            entity.Property(c => c.CompanyName).HasColumnName("companyName").HasMaxLength(255).IsRequired();
            entity.Property(c => c.ActivityField).HasColumnName("activityField").HasMaxLength(255);
            entity.Property(c => c.CoverImage).HasColumnName("coverImage").HasMaxLength(500);
            entity.Property(c => c.Avatar).HasColumnName("avatar").HasMaxLength(500);
            entity.Property(c => c.TaxIdentification).HasColumnName("taxIdentification");
            entity.Property(c => c.Address).HasColumnName("address").HasMaxLength(500);
            entity.Property(c => c.Description).HasColumnName("description");

            // Unique index on userId
            entity.HasIndex(c => c.UserId).IsUnique().HasDatabaseName("IX_company_userId");
        });

        modelBuilder.Entity<Expert>(entity =>
        {
            entity.ToTable("expert");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasColumnName("expertId");
            entity.Property(e => e.UserId).HasColumnName("userId").IsRequired();
            entity.Property(e => e.Name).HasColumnName("name").HasMaxLength(255).IsRequired();
            entity.Property(e => e.Phone).HasColumnName("phone").HasMaxLength(50);
            entity.Property(e => e.CoverImage).HasColumnName("coverImage").HasMaxLength(500);
            entity.Property(e => e.Avatar).HasColumnName("avatar").HasMaxLength(500);

            entity.HasIndex(e => e.UserId).IsUnique().HasDatabaseName("IX_expert_userId");
        });
    }
}
